using Cart.Domain.CartAggregate;

namespace Cart.Application.Abstractions;

public interface ICartRepository
{
    Task<ShoppingCart?> GetCartAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task DeleteCartAsync(string userId, CancellationToken cancellationToken = default);
}

