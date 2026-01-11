using Order.Application.Common.Models;

namespace Order.Application.Common.Interfaces;

public interface ICatalogService
{
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> GetProductsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    Task<bool> CheckStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}

