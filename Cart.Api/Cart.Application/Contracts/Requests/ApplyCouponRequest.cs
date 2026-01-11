namespace Cart.Application.Contracts.Requests;

public record ApplyCouponRequest(
    string CouponCode,
    string CouponType,
    decimal Value,
    decimal? MinimumOrderAmount = null,
    DateTime? ExpiresAt = null
);

