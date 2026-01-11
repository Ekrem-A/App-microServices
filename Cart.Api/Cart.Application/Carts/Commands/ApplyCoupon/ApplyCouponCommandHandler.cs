using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using Cart.Domain.CartAggregate;
using MediatR;

namespace Cart.Application.Carts.Commands.ApplyCoupon;

public class ApplyCouponCommandHandler : IRequestHandler<ApplyCouponCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public ApplyCouponCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(ApplyCouponCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
            throw new InvalidOperationException($"Cart is empty or not found for user {request.UserId}.");

        var couponType = Enum.Parse<CouponType>(request.CouponType, ignoreCase: true);
        var coupon = new Coupon(
            request.CouponCode,
            couponType,
            request.Value,
            request.MinimumOrderAmount,
            request.ExpiresAt
        );

        cart.ApplyCoupon(coupon);

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

