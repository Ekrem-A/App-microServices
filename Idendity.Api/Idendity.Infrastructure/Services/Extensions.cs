using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Idendity.Infrastructure.Services;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring OpenTelemetry, service discovery, resilience, and health checks.
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<OpenTelemetryOption>(builder.Configuration.GetSection("OpenTelemetryOption"));

        var openTelemetryOptions = builder.Configuration.GetSection("OpenTelemetryOption").Get<OpenTelemetryOption>()
            ?? new OpenTelemetryOption
            {
                ServiceName = "IdentityApi",
                ServiceVersion = "1.0.0",
                ActivitySourceName = "IdentityApi.ActivitySource"
            };

        ActivitySourceProvider.Source = new System.Diagnostics.ActivitySource(openTelemetryOptions.ActivitySourceName);

        var otelBuilder = builder.Services.AddOpenTelemetry();

        // Configure Tracing
        otelBuilder.WithTracing(tracing =>
        {
            tracing
                .AddSource(openTelemetryOptions.ActivitySourceName)
                .ConfigureResource(resource =>
                {
                    resource.AddService(
                        serviceName: openTelemetryOptions.ServiceName,
                        serviceVersion: openTelemetryOptions.ServiceVersion);
                    resource.AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    });
                })
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health") &&
                        !httpContext.Request.Path.StartsWithSegments("/alive");
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                });

            // Add Redis instrumentation if Redis is configured
            try
            {
                tracing.AddRedisInstrumentation(redisOptions =>
                {
                    redisOptions.SetVerboseDatabaseStatements = true;
                });
            }
            catch
            {
                // Redis not configured, skip
            }

            // Console exporter for development
            if (builder.Environment.IsDevelopment())
            {
                tracing.AddConsoleExporter();
            }

            // OTLP exporter for production (OpenTelemetry Collector)
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

        // Configure Metrics
        otelBuilder.WithMetrics(metrics =>
        {
            metrics
                .ConfigureResource(resource =>
                {
                    resource.AddService(
                        serviceName: openTelemetryOptions.ServiceName,
                        serviceVersion: openTelemetryOptions.ServiceVersion);
                })
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("System.Net.Http");

            // Console exporter for development
            if (builder.Environment.IsDevelopment())
            {
                metrics.AddConsoleExporter();
            }

            // OTLP exporter
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

        // Configure Logging
        otelBuilder.WithLogging(logging =>
        {
            logging.ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: openTelemetryOptions.ServiceName,
                    serviceVersion: openTelemetryOptions.ServiceVersion);
            });

            // OTLP exporter
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                logging.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }
        });

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath);

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
