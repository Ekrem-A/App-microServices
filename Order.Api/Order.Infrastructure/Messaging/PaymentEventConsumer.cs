using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Application.Common.IntegrationEvents;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Kafka consumer that listens to payment-succeeded and payment-failed topics.
/// Updates Order status accordingly.
/// 
/// Flow: Payment.Api → Kafka (payment-succeeded/failed) → PaymentEventConsumer → Update Order status
/// </summary>
public class PaymentEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _settings;
    private readonly ILogger<PaymentEventConsumer> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentEventConsumer(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> settings,
        ILogger<PaymentEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Don't block startup

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_settings.AutoOffsetReset, true),
            EnableAutoCommit = _settings.EnableAutoCommit,
            SessionTimeoutMs = _settings.SessionTimeoutMs,
            HeartbeatIntervalMs = _settings.HeartbeatIntervalMs
        };

        if (!string.IsNullOrEmpty(_settings.SaslUsername))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol, true);
            config.SaslMechanism = Enum.Parse<SaslMechanism>(_settings.SaslMechanism, true);
            config.SaslUsername = _settings.SaslUsername;
            config.SaslPassword = _settings.SaslPassword;
        }

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        // Subscribe to both payment topics
        consumer.Subscribe(new[]
        {
            _settings.PaymentSucceededTopic,
            _settings.PaymentFailedTopic
        });

        _logger.LogInformation(
            "🎧 PaymentEventConsumer started. Subscribed to [{Topics}]",
            $"{_settings.PaymentSucceededTopic}, {_settings.PaymentFailedTopic}");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value == null)
                        continue;

                    _logger.LogInformation(
                        "📥 Received message from {Topic} [Key={Key}, Partition={Partition}, Offset={Offset}]",
                        consumeResult.Topic,
                        consumeResult.Message.Key,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult.Topic, consumeResult.Message.Value, stoppingToken);

                    // Manual commit after successful processing
                    if (!_settings.EnableAutoCommit)
                    {
                        consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing payment event");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string topic, string messageValue, CancellationToken ct)
    {
        if (topic == _settings.PaymentSucceededTopic)
        {
            var @event = JsonSerializer.Deserialize<PaymentSucceededIntegrationEvent>(messageValue, s_jsonOptions);
            if (@event != null)
            {
                await HandlePaymentSucceeded(@event, ct);
            }
        }
        else if (topic == _settings.PaymentFailedTopic)
        {
            var @event = JsonSerializer.Deserialize<PaymentFailedIntegrationEvent>(messageValue, s_jsonOptions);
            if (@event != null)
            {
                await HandlePaymentFailed(@event, ct);
            }
        }
        else
        {
            _logger.LogWarning("Unknown topic: {Topic}", topic);
        }
    }

    private async Task HandlePaymentSucceeded(PaymentSucceededIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "✅ Processing PaymentSucceeded for Order {OrderId}, Amount: {Amount} {Currency}",
            @event.OrderId, @event.PaidAmount, @event.Currency);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId, ct);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for PaymentSucceeded event", @event.OrderId);
            return;
        }

        order.MarkPaymentCompleted();
        order.MarkAsProcessing();

        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "✅ Order {OrderId} updated: PaymentStatus=Completed, Status=Processing",
            @event.OrderId);
    }

    private async Task HandlePaymentFailed(PaymentFailedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "❌ Processing PaymentFailed for Order {OrderId}, Reason: {Reason}",
            @event.OrderId, @event.ReasonMessage);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId, ct);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for PaymentFailed event", @event.OrderId);
            return;
        }

        order.MarkPaymentFailed();

        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "❌ Order {OrderId} updated: PaymentStatus=Failed, Status=Failed",
            @event.OrderId);
    }
}
