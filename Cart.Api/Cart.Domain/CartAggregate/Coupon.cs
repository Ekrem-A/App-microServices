namespace Cart.Domain.CartAggregate;

public class Coupon
{
    public string Code { get; private set; } = string.Empty;
    public CouponType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private Coupon() { }

    public Coupon(string code, CouponType type, decimal value, decimal? minimumOrderAmount = null, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Coupon code cannot be empty.", nameof(code));
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Coupon value must be greater than zero.");
        if (type == CouponType.Percentage && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage discount cannot exceed 100%.");

        Code = code.ToUpperInvariant();
        Type = type;
        Value = value;
        MinimumOrderAmount = minimumOrderAmount;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    public bool IsApplicable(decimal orderTotal)
    {
        if (IsExpired())
            return false;

        if (MinimumOrderAmount.HasValue && orderTotal < MinimumOrderAmount.Value)
            return false;

        return true;
    }

    public decimal CalculateDiscount(decimal orderTotal)
    {
        if (!IsApplicable(orderTotal))
            return 0;

        return Type switch
        {
            CouponType.Percentage => Math.Round(orderTotal * (Value / 100), 2),
            CouponType.FixedAmount => Math.Min(Value, orderTotal),
            _ => 0
        };
    }
}

public enum CouponType
{
    Percentage,
    FixedAmount
}

