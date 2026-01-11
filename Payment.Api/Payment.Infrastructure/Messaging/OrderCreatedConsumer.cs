using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Events;
using Payment.Application.Services;
using Payment.Infrastructure.Configuration;

namespace Payment.Infrastructure.Messaging;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _settings;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public OrderCreatedConsumer(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> settings,
        ILogger<OrderCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block startup

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_settings.AutoOffsetReset, true),
            EnableAutoCommit = _settings.EnableAutoCommit,
            SessionTimeoutMs = _settings.SessionTimeoutMs,
            HeartbeatIntervalMs = _settings.HeartbeatIntervalMs
        };

        // Add SASL configuration if provided
        if (!string.IsNullOrEmpty(_settings.SaslUsername))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol, true);
            config.SaslMechanism = Enum.Parse<SaslMechanism>(_settings.SaslMechanism, true);
            config.SaslUsername = _settings.SaslUsername;
            config.SaslPassword = _settings.SaslPassword;
        }

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        _consumer.Subscribe(_settings.OrderCreatedTopic);
        
        _logger.LogInformation("Started consuming from topic {Topic}", _settings.OrderCreatedTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message?.Value == null)
                        continue;

                    _logger.LogInformation("Received OrderCreated message: Key={Key}, Partition={Partition}, Offset={Offset}",
                        consumeResult.Message.Key,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                    // Manual commit after successful processing
                    if (!_settings.EnableAutoCommit)
                    {
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    // Continue consuming other messages
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message");
                    // Add delay before retrying
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task ProcessMessageAsync(string messageValue, CancellationToken cancellationToken)
    {
        var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(messageValue, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (orderEvent == null)
        {
            _logger.LogWarning("Failed to deserialize OrderCreatedEvent");
            return;
        }

        _logger.LogInformation("Processing OrderCreated for OrderId {OrderId}, Amount {Amount}",
            orderEvent.OrderId, orderEvent.TotalAmount);

        using var scope = _serviceProvider.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();

        try
        {
            var (success, iframeUrl, error) = await paymentService.StartPaymentForOrderAsync(orderEvent, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Payment initiated for OrderId {OrderId}, IframeUrl: {IframeUrl}",
                    orderEvent.OrderId, iframeUrl);
            }
            else
            {
                _logger.LogWarning("Failed to initiate payment for OrderId {OrderId}: {Error}",
                    orderEvent.OrderId, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreated for OrderId {OrderId}", orderEvent.OrderId);
            throw; // Re-throw to prevent commit
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

