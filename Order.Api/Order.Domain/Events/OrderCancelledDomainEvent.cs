using Order.Domain.Common;

namespace Order.Domain.Events;

public sealed record OrderCancelledDomainEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string UserId { get; }
    public string? Reason { get; }

    public OrderCancelledDomainEvent(Guid orderId, string userId, string? reason)
    {
        OrderId = orderId;
        UserId = userId;
        Reason = reason;
    }
}

