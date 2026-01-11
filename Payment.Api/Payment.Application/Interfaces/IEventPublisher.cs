namespace Payment.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken cancellationToken = default) where T : class;
}

