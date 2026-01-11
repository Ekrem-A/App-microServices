using Order.Application.Common.Models;

namespace Order.Application.Common.Interfaces;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(string userId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(string userId, CancellationToken cancellationToken = default);
}


