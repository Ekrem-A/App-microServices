using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Application.Common.Interfaces;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Kafka-based event publisher for the Outbox pattern.
/// Publishes integration events to Kafka topics based on event type.
/// 
/// Flow: DB Transaction (Order + OutboxMessage) → OutboxProcessorJob → KafkaEventPublisher → Kafka
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private bool _disposed;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaEventPublisher(IOptions<KafkaSettings> settings, ILogger<KafkaEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            LingerMs = 5 // Batch for better throughput
        };

        // Add SASL if configured
        if (!string.IsNullOrEmpty(_settings.SaslUsername))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol, true);
            config.SaslMechanism = Enum.Parse<SaslMechanism>(_settings.SaslMechanism, true);
            config.SaslUsername = _settings.SaslUsername;
            config.SaslPassword = _settings.SaslPassword;
        }

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Reason}", e.Reason))
            .Build();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var topic = ResolveTopicName(@event);
        var key = ResolveMessageKey(@event);
        var json = JsonSerializer.Serialize(@event, s_jsonOptions);

        var message = new Message<string, string>
        {
            Key = key,
            Value = json,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(typeof(TEvent).Name) },
                { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
            }
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "📤 Published {EventType} to {Topic} [Partition={Partition}, Offset={Offset}]",
                typeof(TEvent).Name, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to {Topic}", typeof(TEvent).Name, topic);
            throw;
        }
    }

    private string ResolveTopicName<TEvent>(TEvent @event) where TEvent : class
    {
        return typeof(TEvent).Name switch
        {
            "OrderCreatedIntegrationEvent" => _settings.OrderCreatedTopic,
            "OrderCancelledIntegrationEvent" => _settings.OrderCancelledTopic,
            _ => $"order-events-{typeof(TEvent).Name.ToLowerInvariant()}"
        };
    }

    private static string ResolveMessageKey<TEvent>(TEvent @event) where TEvent : class
    {
        // Use OrderId as the key for Kafka partitioning (same order always same partition)
        var orderIdProp = typeof(TEvent).GetProperty("OrderId");
        if (orderIdProp?.GetValue(@event) is Guid orderId)
            return orderId.ToString();

        return Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        _disposed = true;
    }
}
