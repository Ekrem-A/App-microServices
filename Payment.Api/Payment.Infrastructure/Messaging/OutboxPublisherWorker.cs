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
        using var scope = _serviceProvider.CreateScope();
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
                var topic = GetTopicForMessageType(message.Type);
                
                // Deserialize and publish based on message type
                object? eventObject = message.Type switch
                {
                    nameof(PaymentSucceededEvent) => JsonSerializer.Deserialize<PaymentSucceededEvent>(message.Payload),
                    nameof(PaymentFailedEvent) => JsonSerializer.Deserialize<PaymentFailedEvent>(message.Payload),
                    nameof(RefundCompletedEvent) => JsonSerializer.Deserialize<RefundCompletedEvent>(message.Payload),
                    nameof(RefundFailedEvent) => JsonSerializer.Deserialize<RefundFailedEvent>(message.Payload),
                    _ => null
                };

                if (eventObject == null)
                {
                    _logger.LogWarning("Unknown message type: {Type}", message.Type);
                    message.MarkAsFailed($"Unknown message type: {message.Type}");
                    await unitOfWork.OutboxMessages.UpdateAsync(message, cancellationToken);
                    continue;
                }

                await PublishEventAsync(eventPublisher, topic, eventObject, message.CorrelationId, cancellationToken);
                
                message.MarkAsProcessed();
                await unitOfWork.OutboxMessages.UpdateAsync(message, cancellationToken);
                
                _logger.LogDebug("Published outbox message {MessageId} of type {Type} to topic {Topic}",
                    message.MessageId, message.Type, topic);
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

    private string GetTopicForMessageType(string messageType)
    {
        return messageType switch
        {
            nameof(PaymentSucceededEvent) => _settings.PaymentSucceededTopic,
            nameof(PaymentFailedEvent) => _settings.PaymentFailedTopic,
            nameof(RefundCompletedEvent) => _settings.RefundCompletedTopic,
            nameof(RefundFailedEvent) => _settings.RefundFailedTopic,
            _ => throw new InvalidOperationException($"No topic configured for message type: {messageType}")
        };
    }

    private async Task PublishEventAsync(IEventPublisher publisher, string topic, object eventObject, string? correlationId, CancellationToken cancellationToken)
    {
        var method = typeof(IEventPublisher).GetMethod(nameof(IEventPublisher.PublishAsync));
        var genericMethod = method!.MakeGenericMethod(eventObject.GetType());
        
        var task = (Task)genericMethod.Invoke(publisher, new object?[] { topic, eventObject, correlationId, cancellationToken })!;
        await task;
    }
}

