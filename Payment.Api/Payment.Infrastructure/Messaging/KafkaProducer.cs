using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Configuration;

namespace Payment.Infrastructure.Messaging;

public class KafkaProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaSettings> settings, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        // Add SASL configuration if provided
        if (!string.IsNullOrEmpty(settings.Value.SaslUsername))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(settings.Value.SecurityProtocol, true);
            config.SaslMechanism = Enum.Parse<SaslMechanism>(settings.Value.SaslMechanism, true);
            config.SaslUsername = settings.Value.SaslUsername;
            config.SaslPassword = settings.Value.SaslPassword;
        }

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = json
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            
            _logger.LogDebug("Published message to {Topic}, Partition: {Partition}, Offset: {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error publishing message to {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}

