using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RepriceCart;

public class RepriceCartCommandHandler : IRequestHandler<RepriceCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public RepriceCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(RepriceCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null)
            throw new InvalidOperationException($"Cart not found for user {request.UserId}.");

        foreach (var update in request.PriceUpdates)
        {
            cart.RepriceItem(update.ProductId, update.NewUnitPrice);
        }

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

