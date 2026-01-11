using Catalog.Application.Common.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Catalog.Infrastructure.Messaging;

public class KafkaProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            ClientId = "catalog-service",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 30000,
            RetryBackoffMs = 1000,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka Error: {Reason}", e.Reason))
            .Build();
    }

    public async Task PublishAsync<T>(string topic, T @event, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var eventJson = JsonSerializer.Serialize(@event);
            var key = Guid.NewGuid().ToString();
            var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

            var message = new Message<string, string>
            {
                Key = key,
                Value = eventJson,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(typeof(T).Name) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")) },
                    { "correlation-id", Encoding.UTF8.GetBytes(correlationId) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} to topic {Topic} at offset {Offset}",
                typeof(T).Name,
                topic,
                result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to topic {Topic}", typeof(T).Name, topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}