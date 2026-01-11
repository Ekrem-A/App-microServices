using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using System.Text.Json;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Simple event publisher that logs events.
/// Can be replaced with RabbitMQ, Redis Pub/Sub, or any other messaging system later.
/// </summary>
public class LogEventPublisher : IEventPublisher
{
    private readonly ILogger<LogEventPublisher> _logger;

    public LogEventPublisher(ILogger<LogEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventType = typeof(TEvent).Name;
        var eventData = JsonSerializer.Serialize(@event, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        _logger.LogInformation(
            "📤 Event Published: {EventType}\n{EventData}", 
            eventType, 
            eventData);

        // TODO: Implement actual messaging (RabbitMQ, Redis, etc.) when needed
        // For now, events are logged and can be consumed by other services via logs or webhooks

        return Task.CompletedTask;
    }
}

