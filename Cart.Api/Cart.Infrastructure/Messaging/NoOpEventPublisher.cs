using Cart.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Messaging;

/// <summary>
/// A no-operation event publisher that logs events instead of publishing to Kafka.
/// Used when Kafka is not configured (e.g., Railway deployment without Kafka).
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : class
    {
        _logger.LogWarning(
            "Kafka is not configured. Event {EventType} was not published. Event data: {@Event}",
            typeof(TEvent).Name,
            @event
        );
        
        return Task.CompletedTask;
    }
}

