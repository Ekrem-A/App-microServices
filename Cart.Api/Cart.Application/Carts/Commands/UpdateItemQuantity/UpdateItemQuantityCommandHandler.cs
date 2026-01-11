using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Commands.UpdateItemQuantity;

public class UpdateItemQuantityCommandHandler : IRequestHandler<UpdateItemQuantityCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public UpdateItemQuantityCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(UpdateItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);

        if (cart is null)
            throw new InvalidOperationException($"Cart not found for user {request.UserId}.");

        cart.UpdateItemQuantity(request.ProductId, request.NewQuantity);

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

