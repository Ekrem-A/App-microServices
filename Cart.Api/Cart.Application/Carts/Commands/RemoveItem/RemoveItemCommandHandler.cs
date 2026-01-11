using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.RemoveItem;

public class RemoveItemCommandHandler : IRequestHandler<RemoveItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public RemoveItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(RemoveItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null)
            throw new InvalidOperationException($"Cart not found for user {request.UserId}.");

        cart.RemoveItem(request.ProductId);

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

