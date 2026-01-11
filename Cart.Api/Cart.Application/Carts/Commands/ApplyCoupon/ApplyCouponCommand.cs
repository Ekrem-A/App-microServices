using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.ApplyCoupon;

public record ApplyCouponCommand(
    string UserId,
    string CouponCode,
    string CouponType,
    decimal Value,
    decimal? MinimumOrderAmount = null,
    DateTime? ExpiresAt = null
) : IRequest<CartDto>;

