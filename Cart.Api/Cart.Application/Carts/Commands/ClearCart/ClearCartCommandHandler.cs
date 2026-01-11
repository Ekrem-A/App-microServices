using Cart.Application.Abstractions;
using MediatR;

namespace Cart.Application.Carts.Commands.ClearCart;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Unit>
{
    private readonly ICartRepository _cartRepository;

    public ClearCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Unit> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _cartRepository.DeleteCartAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

