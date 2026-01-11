namespace Cart.Application.DTOs;

public record CartDto(
    string UserId,
    List<CartItemDto> Items,
    CouponDto? AppliedCoupon,
    decimal SubTotal,
    decimal Discount,
    decimal Total,
    int TotalItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CartItemDto(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record CouponDto(
    string Code,
    string Type,
    decimal Value,
    decimal? MinimumOrderAmount,
    DateTime? ExpiresAt
);

