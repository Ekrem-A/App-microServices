using Cart.Application.Abstractions;
using Cart.Application.DTOs;
using Cart.Domain.CartAggregate;
using MediatR;

namespace Cart.Application.Carts.Commands.MergeCart;

public class MergeCartCommandHandler : IRequestHandler<MergeCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;

    public MergeCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> Handle(MergeCartCommand request, CancellationToken cancellationToken)
    {
        var sourceCart = await _cartRepository.GetCartAsync(request.SourceUserId, cancellationToken);

        if (sourceCart is null || sourceCart.Items.Count == 0)
        {
            // Nothing to merge, return target cart or empty
            var existingTarget = await _cartRepository.GetCartAsync(request.TargetUserId, cancellationToken);
            return existingTarget?.ToDto() ?? new CartDto(
                request.TargetUserId,
                new List<CartItemDto>(),
                null,
                0, 0, 0, 0,
                DateTime.UtcNow,
                DateTime.UtcNow
            );
        }

        var targetCart = await _cartRepository.GetCartAsync(request.TargetUserId, cancellationToken)
                         ?? new ShoppingCart(request.TargetUserId);

        targetCart.MergeFrom(sourceCart);

        await _cartRepository.SaveCartAsync(targetCart, cancellationToken);

        // Delete source cart after merge
        await _cartRepository.DeleteCartAsync(request.SourceUserId, cancellationToken);

        return targetCart.ToDto();
    }
}

