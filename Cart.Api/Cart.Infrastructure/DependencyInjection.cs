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

        // Redis connection - supports Railway REDIS_URL (Lazy initialization)
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
        // Check for Railway's REDIS_URL environment variable first
        var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
        
        if (!string.IsNullOrEmpty(redisUrl))
        {
            // Railway format: redis://default:password@host:port
            // StackExchange.Redis format: host:port,password=password
            try
            {
                var uri = new Uri(redisUrl);
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 6379;
                var password = uri.UserInfo.Contains(':') 
                    ? uri.UserInfo.Split(':')[1] 
                    : uri.UserInfo;
                
                if (!string.IsNullOrEmpty(password))
                {
                    return $"{host}:{port},password={password},abortConnect=false";
                }
                return $"{host}:{port},abortConnect=false";
            }
            catch
            {
                // If parsing fails, try using it as-is
                return redisUrl;
            }
        }
        
        // Fallback to configuration
        return configuration.GetSection($"{RedisOptions.SectionName}:ConnectionString").Value
               ?? "localhost:6379";
    }
}

