using Order.Domain.Common;
using Order.Domain.Entities;

namespace Order.Domain.Events;

public sealed record OrderCreatedDomainEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string UserId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }
    public int ItemCount { get; }

    public OrderCreatedDomainEvent(Entities.Order order)
    {
        OrderId = order.Id;
        UserId = order.UserId;
        TotalAmount = order.TotalAmount.Amount;
        Currency = order.TotalAmount.Currency;
        ItemCount = order.Items.Count;
    }
}

