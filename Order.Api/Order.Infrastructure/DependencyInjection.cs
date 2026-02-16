using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Infrastructure.BackgroundJobs;
using Order.Infrastructure.ExternalServices;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - SQL Server
        var connectionString = configuration.GetConnectionString("OrderDb");
        
        services.AddDbContext<OrderDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            }
            else
            {
                // Use in-memory database for development/testing when no connection string is provided
                options.UseInMemoryDatabase("OrderDb");
            }
        });

        services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());

        // Database Initializer
        

        // Services
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // Kafka Settings
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        var kafkaSettings = configuration.GetSection(KafkaSettings.SectionName).Get<KafkaSettings>();

        if (kafkaSettings?.IsEnabled == true)
        {
            // Kafka-based event publisher (Production)
            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

            // Kafka consumer: listens to payment-succeeded / payment-failed
            services.AddHostedService<PaymentEventConsumer>();
        }
        else
        {
            // Log-based publisher (Development without Kafka)
            services.AddSingleton<IEventPublisher, LogEventPublisher>();
        }

        // Background Jobs
        services.AddHostedService<OutboxProcessorJob>();

        // External Services with Polly resilience
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        services.AddHttpClient<ICartService, CartServiceClient>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

        services.AddHttpClient<ICatalogService, CatalogServiceClient>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

        return services;
    }
}

