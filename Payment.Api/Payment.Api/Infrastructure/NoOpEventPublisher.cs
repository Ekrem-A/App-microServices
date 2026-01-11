using Payment.Application.Interfaces;
using Serilog;

namespace Payment.Api.Infrastructure;

/// <summary>
/// No-op implementation of IEventPublisher when Kafka is disabled
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken cancellationToken = default) where T : class
    {
        Log.Warning("Kafka is not configured. Message to topic {Topic} was not published.", topic);
        return Task.CompletedTask;
    }
}
