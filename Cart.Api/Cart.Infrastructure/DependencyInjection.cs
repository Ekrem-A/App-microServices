using Cart.Application.Abstractions;
using Cart.Infrastructure.Messaging;
using Cart.Infrastructure.Options;
using Cart.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<CartOptions>(configuration.GetSection(CartOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        // Redis connection
        var redisConnectionString = GetRedisConnectionString(configuration);
        Console.WriteLine($"Redis connection string configured (host hidden for security)");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            try
            {
                Console.WriteLine("Attempting to connect to Redis...");
                var configOptions = ConfigurationOptions.Parse(redisConnectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.ConnectTimeout = 15000; // 15 seconds
                configOptions.SyncTimeout = 15000;
                configOptions.AsyncTimeout = 15000;
                configOptions.ConnectRetry = 5;
                configOptions.ReconnectRetryPolicy = new LinearRetry(2000);
                
                var connection = ConnectionMultiplexer.Connect(configOptions);
                Console.WriteLine($"Redis connected: {connection.IsConnected}");
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis connection error: {ex.Message}");
                throw;
            }
        });

        // Repositories
        services.AddScoped<ICartRepository, RedisCartRepository>();

        // Event Publisher - Use Kafka if configured, otherwise use NoOp (log-only)
        var kafkaBootstrapServers = configuration.GetSection("Kafka:BootstrapServers").Value;
        var useKafka = !string.IsNullOrEmpty(kafkaBootstrapServers) && kafkaBootstrapServers != "localhost:9092"
                       || Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") != null;
        
        if (useKafka)
        {
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
        }

        return services;
    }

    private static string GetRedisConnectionString(IConfiguration configuration)
    {
        return configuration.GetSection($"{RedisOptions.SectionName}:ConnectionString").Value
               ?? "localhost:6379";
    }
}

