namespace Order.Application.Common.IntegrationEvents;

public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string UserId,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    DateTime OccurredOnUtc,
    IEnumerable<OrderItemIntegrationEvent> Items);

public record OrderItemIntegrationEvent(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

