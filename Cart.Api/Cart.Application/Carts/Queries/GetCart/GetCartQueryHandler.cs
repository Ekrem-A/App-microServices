using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using MediatR;

namespace Cart.Application.Carts.Queries.GetCart;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(request.UserId, cancellationToken);
        return cart?.ToDto();
    }
}

