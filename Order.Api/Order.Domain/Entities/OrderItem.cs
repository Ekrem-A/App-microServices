using Order.Domain.Common;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductImageUrl { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money TotalPrice { get; private set; } = null!;

    private OrderItem() { }

    internal OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        string? productImageUrl,
        int quantity,
        Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        OrderId = orderId;
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        ProductImageUrl = productImageUrl;
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        TotalPrice = unitPrice.Multiply(quantity);
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        Quantity = newQuantity;
        TotalPrice = UnitPrice.Multiply(newQuantity);
    }
}

