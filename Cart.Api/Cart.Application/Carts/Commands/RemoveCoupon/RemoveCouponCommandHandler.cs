using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RemoveCoupon;

public class RemoveCouponCommandHandler : IRequestHandler<RemoveCouponCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public RemoveCouponCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(RemoveCouponCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null)
            throw new InvalidOperationException($"Cart not found for user {request.UserId}.");

        cart.RemoveCoupon();

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

