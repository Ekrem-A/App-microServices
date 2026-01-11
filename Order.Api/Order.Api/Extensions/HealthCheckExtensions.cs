using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Api.HealthChecks;
using System.Text.Json;

namespace Order.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check - PostgreSQL for Railway
        var dbConnectionString = configuration.GetConnectionString("OrderDb");
        if (!string.IsNullOrWhiteSpace(dbConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                dbConnectionString,
                name: "postgresql",
                tags: new[] { "ready", "db" });
        }
        else
        {
            healthChecksBuilder.AddCheck<DatabaseHealthCheck>(
                "database",
                tags: new[] { "ready", "db" });
        }

        return services;
    }

    public static IEndpointRouteBuilder MapCustomHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe - basic check that app is running
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks, just confirms app is running
            ResponseWriter = WriteResponse
        });

        // Readiness probe - checks all dependencies
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        });

        // Detailed health check (protected)
        endpoints.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse
        }).RequireAuthorization();

        return endpoints;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        return context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

