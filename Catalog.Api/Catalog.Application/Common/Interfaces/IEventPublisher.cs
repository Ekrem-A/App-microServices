namespace Catalog.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T @event, CancellationToken cancellationToken = default)
        where T : class;
}
