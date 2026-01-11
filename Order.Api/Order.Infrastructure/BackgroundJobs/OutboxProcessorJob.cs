using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.Common.IntegrationEvents;
using Order.Application.Common.Interfaces;
using Order.Domain.Events;
using Order.Infrastructure.Persistence;
using System.Text.Json;

namespace Order.Infrastructure.BackgroundJobs;

public class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);

    public OutboxProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor job stopped");
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = ConvertToIntegrationEvent(message);
                
                if (integrationEvent != null)
                {
                    await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogInformation("Processed outbox message {MessageId} of type {Type}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to process outbox message {MessageId}, retry count: {RetryCount}", 
                    message.Id, message.RetryCount);
            }
        }

        if (messages.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private object? ConvertToIntegrationEvent(OutboxMessage message)
    {
        // Map domain events to integration events
        if (message.Type.Contains(nameof(OrderCreatedDomainEvent)))
        {
            var domainEvent = JsonSerializer.Deserialize<OrderCreatedDomainEventDto>(message.Content);
            if (domainEvent != null)
            {
                return new OrderCreatedIntegrationEvent(
                    domainEvent.OrderId,
                    domainEvent.UserId,
                    domainEvent.TotalAmount,
                    domainEvent.Currency,
                    domainEvent.ItemCount,
                    domainEvent.OccurredOnUtc,
                    Enumerable.Empty<OrderItemIntegrationEvent>());
            }
        }
        else if (message.Type.Contains(nameof(OrderCancelledDomainEvent)))
        {
            var domainEvent = JsonSerializer.Deserialize<OrderCancelledDomainEventDto>(message.Content);
            if (domainEvent != null)
            {
                return new OrderCancelledIntegrationEvent(
                    domainEvent.OrderId,
                    domainEvent.UserId,
                    domainEvent.Reason,
                    domainEvent.OccurredOnUtc);
            }
        }

        _logger.LogWarning("Unknown event type: {Type}", message.Type);
        return null;
    }

    // DTOs for deserialization
    private record OrderCreatedDomainEventDto(
        Guid EventId,
        DateTime OccurredOnUtc,
        Guid OrderId,
        string UserId,
        decimal TotalAmount,
        string Currency,
        int ItemCount);

    private record OrderCancelledDomainEventDto(
        Guid EventId,
        DateTime OccurredOnUtc,
        Guid OrderId,
        string UserId,
        string? Reason);
}

