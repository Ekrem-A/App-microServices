using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using Cart.Domain.CartAggregate;
using MediatR;

namespace Cart.Application.Carts.Commands.AddItem;

public class AddItemCommandHandler : IRequestHandler<AddItemCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public AddItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(AddItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken)
                   ?? new ShoppingCart(request.UserId);

        cart.AddItem(
            request.ProductId,
            request.ProductName,
            request.UnitPrice,
            request.Quantity,
            request.ImageUrl
        );

        await _cartRepository.SaveCartAsync(cart, cancellationToken);

        return cart.ToDto();
    }
}

