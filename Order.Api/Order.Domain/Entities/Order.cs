using Order.Domain.Common;
using Order.Domain.Enums;
using Order.Domain.Events;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public string UserId { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public Address? BillingAddress { get; private set; }
    public Money SubTotal { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public string? Notes { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? ShippedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(
        string userId,
        Address shippingAddress,
        Address? billingAddress = null,
        string? notes = null,
        string? idempotencyKey = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var order = new Order
        {
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress)),
            BillingAddress = billingAddress,
            SubTotal = Money.Zero(),
            ShippingCost = Money.Zero(),
            TotalAmount = Money.Zero(),
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        };

        return order;
    }

    public void AddItem(Guid productId, string productName, string? productImageUrl, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify a non-pending order");

        var existingItem = _items.FirstOrDefault(x => x.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = new OrderItem(Id, productId, productName, productImageUrl, quantity, unitPrice);
            _items.Add(item);
        }

        RecalculateTotals();
    }

    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify a non-pending order");

        var item = _items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotals();
        }
    }

    public void SetShippingCost(Money shippingCost)
    {
        ShippingCost = shippingCost ?? throw new ArgumentNullException(nameof(shippingCost));
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubTotal = _items.Any()
            ? _items.Select(x => x.TotalPrice).Aggregate((a, b) => a.Add(b))
            : Money.Zero();

        TotalAmount = SubTotal.Add(ShippingCost);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");

        if (!_items.Any())
            throw new InvalidOperationException("Cannot confirm an empty order");

        Status = OrderStatus.Confirmed;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new OrderCreatedDomainEvent(this));
    }

    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot process order in {Status} status");

        Status = OrderStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPaymentCompleted()
    {
        PaymentStatus = PaymentStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        Status = OrderStatus.Failed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException($"Cannot ship order in {Status} status");

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number cannot be empty", nameof(trackingNumber));

        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        ShippedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot mark as delivered order in {Status} status");

        Status = OrderStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            Notes = $"{Notes}\nCancellation reason: {reason}".Trim();

        AddDomainEvent(new OrderCancelledDomainEvent(Id, UserId, reason));
    }
}

