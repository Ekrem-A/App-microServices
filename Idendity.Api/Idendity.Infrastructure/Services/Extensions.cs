using KubernetesLessons.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
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

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<OpenTelemetryOption>(builder.Configuration.GetSection("OpenTelemetryOption"));
        var openTelemetryConstants =
            (builder.Configuration.GetSection("OpenTelemetryOption").Get<OpenTelemetryOption>())!;

        ActivitySourceProvider.Source =
            new System.Diagnostics.ActivitySource(openTelemetryConstants.ActivitySourceName);

        builder.Services.AddOpenTelemetry().WithTracing(options =>
        {
            options.AddSource(openTelemetryConstants.ActivitySourceName)
                .ConfigureResource(resource =>
                {
                    resource.AddService(openTelemetryConstants.ServiceName,
                        serviceVersion: openTelemetryConstants.ServiceVersion);
                });
            options.AddAspNetCoreInstrumentation();
            options.AddHttpClientInstrumentation();

            options.AddEntityFrameworkCoreInstrumentation();
            options.AddRedisInstrumentation(redisOptions => { redisOptions.SetVerboseDatabaseStatements = true; });

            options.AddConsoleExporter();

            var exporter = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(exporter))
            {
                options.AddOtlpExporter(x => { x.Endpoint = new Uri(exporter); });
            }
        }).WithMetrics(metric =>
        {
            metric.ConfigureResource(resource =>
            {
                resource.AddService(openTelemetryConstants.ServiceName,
                    serviceVersion: openTelemetryConstants.ServiceVersion);
            });


            metric.AddAspNetCoreInstrumentation();
            metric.AddHttpClientInstrumentation();
            metric.AddRuntimeInstrumentation();
            metric.AddConsoleExporter();
            var exporter = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(exporter))
            {
                metric.AddOtlpExporter(x => { x.Endpoint = new Uri(exporter); });
            }
        }).WithLogging(logging =>
        {
            logging.ConfigureResource(resource =>
            {
                resource.AddService(openTelemetryConstants.ServiceName,
                    serviceVersion: openTelemetryConstants.ServiceVersion);
            });

            var exporter = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(exporter))
            {
                logging.AddOtlpExporter(x => { x.Endpoint = new Uri(exporter); });
            }
        });


        return builder;
    }


    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
