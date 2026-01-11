namespace Cart.Domain.CartAggregate;

public class CartItem
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    private CartItem() { }

    public CartItem(Guid productId, string productName, decimal unitPrice, int quantity, string? imageUrl = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName cannot be empty.", nameof(productName));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        ImageUrl = imageUrl;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(newQuantity), "Quantity must be greater than zero.");

        Quantity = newQuantity;
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        Quantity += amount;
    }

    public void UpdatePrice(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newUnitPrice), "UnitPrice cannot be negative.");

        UnitPrice = newUnitPrice;
    }
}

