using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Infrastructure.Persistence;

namespace Order.Api.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly OrderDbContext _dbContext;

    public DatabaseHealthCheck(OrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Database connection is healthy");
            }

            return HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}

