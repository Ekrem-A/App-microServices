using Catalog.Application.Common.Interfaces;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Data;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.QueryServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database (PostgreSQL for Railway)
        services.AddDbContext<CatalogDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null
                );
            });
        });

        // Query Services (must be registered before UnitOfWork)
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IBrandQueryService, BrandQueryService>();

        // Unit of Work (Scoped - her request için yeni instance)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Redis Cache (Optional)
        var redisEnabledValue = configuration["Redis:Enabled"];
        var redisEnabled = !string.IsNullOrEmpty(redisEnabledValue) && bool.Parse(redisEnabledValue);
        var redisConnection = configuration["Redis:Configuration"];
        
        if (redisEnabled && !string.IsNullOrEmpty(redisConnection))
        {
            try
            {
                var redisInstanceName = configuration["Redis:InstanceName"] ?? "CatalogCache_";
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConnection!));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            catch
            {
                // Redis bağlantısı başarısız olursa null cache service kullan
                services.AddSingleton<ICacheService, NullCacheService>();
            }
        }
        else
        {
            services.AddSingleton<ICacheService, NullCacheService>();
        }

        // Kafka (Optional)
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrEmpty(kafkaBootstrapServers))
        {
            services.AddSingleton<IEventPublisher, KafkaProducer>();
        }

        return services;
    }
}