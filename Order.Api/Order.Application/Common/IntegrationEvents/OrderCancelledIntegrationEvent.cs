namespace Order.Application.Common.IntegrationEvents;

public record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string UserId,
    string? Reason,
    DateTime OccurredOnUtc);

