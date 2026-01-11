using System.Text.Json;
using Cart.Application.Abstractions;
using Cart.Infrastructure.Options;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cart.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaEventPublisher(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<KafkaEventPublisher> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 30000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var topic = GetTopicForEvent<TEvent>();
        var key = GetKeyForEvent(@event);
        var value = JsonSerializer.Serialize(@event, JsonOptions);

        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Event {EventType} published to topic {Topic} at partition {Partition} offset {Offset}",
                typeof(TEvent).Name,
                result.Topic,
                result.Partition.Value,
                result.Offset.Value
            );
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", typeof(TEvent).Name, topic);
            throw;
        }
    }

    private string GetTopicForEvent<TEvent>()
    {
        // For now, all checkout events go to the same topic
        // Extend this if you have more event types
        return _kafkaOptions.Topics.CheckoutRequested;
    }

    private static string GetKeyForEvent<TEvent>(TEvent @event)
    {
        // Try to extract userId from the event for partitioning
        var userIdProperty = typeof(TEvent).GetProperty("UserId");
        if (userIdProperty?.GetValue(@event) is string userId)
        {
            return userId;
        }

        var eventIdProperty = typeof(TEvent).GetProperty("EventId");
        if (eventIdProperty?.GetValue(@event) is Guid eventId)
        {
            return eventId.ToString();
        }

        return Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        _disposed = true;
    }
}

