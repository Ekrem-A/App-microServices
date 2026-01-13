using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Events;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Configuration;

namespace Payment.Infrastructure.Messaging;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _settings;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;

    // Cached JsonSerializerOptions
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> settings,
        ILogger<OutboxPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        
        _logger.LogInformation("Outbox Publisher Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pendingMessages = await unitOfWork.OutboxMessages.GetPendingMessagesAsync(BatchSize, cancellationToken);
        var messageList = pendingMessages.ToList();

        if (messageList.Count == 0)
            return;

        _logger.LogDebug("Processing {Count} outbox messages", messageList.Count);

        foreach (var message in messageList)
        {
            try
            {
                var (topic, success) = await PublishMessageByTypeAsync(eventPublisher, message.Type, message.Payload, message.CorrelationId, cancellationToken);
                
                if (!success)
                {
                    _logger.LogWarning("Unknown message type: {Type}", message.Type);
                    message.MarkAsFailed($"Unknown message type: {message.Type}");
                }
                else
                {
                    message.MarkAsProcessed();
                    _logger.LogDebug("Published outbox message {MessageId} of type {Type} to topic {Topic}",
                        message.MessageId, message.Type, topic);
                }
                
                await unitOfWork.OutboxMessages.UpdateAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing outbox message {MessageId}", message.MessageId);
                message.MarkAsFailed(ex.Message);
                await unitOfWork.OutboxMessages.UpdateAsync(message, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<(string Topic, bool Success)> PublishMessageByTypeAsync(
        IEventPublisher publisher, 
        string messageType, 
        string payload, 
        string? correlationId,
        CancellationToken cancellationToken)
    {
        // Direct type mapping without reflection
        switch (messageType)
        {
            case nameof(PaymentSucceededEvent):
                var paymentSucceeded = JsonSerializer.Deserialize<PaymentSucceededEvent>(payload, s_jsonOptions);
                if (paymentSucceeded != null)
                {
                    await publisher.PublishAsync(_settings.PaymentSucceededTopic, paymentSucceeded, correlationId, cancellationToken);
                    return (_settings.PaymentSucceededTopic, true);
                }
                break;
                
            case nameof(PaymentFailedEvent):
                var paymentFailed = JsonSerializer.Deserialize<PaymentFailedEvent>(payload, s_jsonOptions);
                if (paymentFailed != null)
                {
                    await publisher.PublishAsync(_settings.PaymentFailedTopic, paymentFailed, correlationId, cancellationToken);
                    return (_settings.PaymentFailedTopic, true);
                }
                break;
                
            case nameof(RefundCompletedEvent):
                var refundCompleted = JsonSerializer.Deserialize<RefundCompletedEvent>(payload, s_jsonOptions);
                if (refundCompleted != null)
                {
                    await publisher.PublishAsync(_settings.RefundCompletedTopic, refundCompleted, correlationId, cancellationToken);
                    return (_settings.RefundCompletedTopic, true);
                }
                break;
                
            case nameof(RefundFailedEvent):
                var refundFailed = JsonSerializer.Deserialize<RefundFailedEvent>(payload, s_jsonOptions);
                if (refundFailed != null)
                {
                    await publisher.PublishAsync(_settings.RefundFailedTopic, refundFailed, correlationId, cancellationToken);
                    return (_settings.RefundFailedTopic, true);
                }
                break;
        }
        
        return (string.Empty, false);
    }
}

