using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Used when running: dotnet ef migrations add [Name] --project Order.Infrastructure --startup-project Order.Api
/// </summary>
public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("OrderDb");

        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
            });
        }
        else
        {
            // Default SQL Server connection for design-time
            optionsBuilder.UseSqlServer(
                "Server=localhost,1436;Database=OrderDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True",
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
                });
        }

        return new OrderDbContext(optionsBuilder.Options);
    }
}