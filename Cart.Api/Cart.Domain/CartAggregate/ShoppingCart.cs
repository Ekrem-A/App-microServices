namespace Cart.Domain.CartAggregate;

public class ShoppingCart
{
    private readonly List<CartItem> _items = new();

    public string UserId { get; private set; } = string.Empty;
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public Coupon? AppliedCoupon { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public decimal SubTotal => _items.Sum(i => i.TotalPrice);
    public decimal Discount => AppliedCoupon?.CalculateDiscount(SubTotal) ?? 0;
    public decimal Total => Math.Max(0, SubTotal - Discount);
    public int TotalItemCount => _items.Sum(i => i.Quantity);

    private ShoppingCart() { }

    public ShoppingCart(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity, string? imageUrl = null)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var newItem = new CartItem(productId, productName, unitPrice, quantity, imageUrl);
            _items.Add(newItem);
        }

        Touch();
    }

    public void UpdateItemQuantity(Guid productId, int newQuantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is null)
            throw new InvalidOperationException($"Item with ProductId {productId} not found in cart.");

        if (newQuantity <= 0)
        {
            _items.Remove(item);
        }
        else
        {
            item.UpdateQuantity(newQuantity);
        }

        Touch();
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is null)
            throw new InvalidOperationException($"Item with ProductId {productId} not found in cart.");

        _items.Remove(item);
        Touch();
    }

    public void Clear()
    {
        _items.Clear();
        AppliedCoupon = null;
        Touch();
    }

    public void ApplyCoupon(Coupon coupon)
    {
        if (coupon is null)
            throw new ArgumentNullException(nameof(coupon));

        if (!coupon.IsApplicable(SubTotal))
            throw new InvalidOperationException($"Coupon '{coupon.Code}' is not applicable to this cart.");

        AppliedCoupon = coupon;
        Touch();
    }

    public void RemoveCoupon()
    {
        AppliedCoupon = null;
        Touch();
    }

    public void MergeFrom(ShoppingCart sourceCart)
    {
        if (sourceCart is null)
            throw new ArgumentNullException(nameof(sourceCart));

        foreach (var item in sourceCart.Items)
        {
            AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.ImageUrl);
        }

        if (AppliedCoupon is null && sourceCart.AppliedCoupon is not null)
        {
            AppliedCoupon = sourceCart.AppliedCoupon;
        }

        Touch();
    }

    public void RepriceItem(Guid productId, decimal newUnitPrice)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is not null)
        {
            item.UpdatePrice(newUnitPrice);
            Touch();
        }
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

