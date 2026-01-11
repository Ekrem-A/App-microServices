using Catalog.Domain.Entities;

namespace Catalog.Application.Common.Interfaces;

public interface IProductQueryService
{
    Task<Product?> GetProductWithIncludesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllProductsWithIncludesAsync(CancellationToken cancellationToken = default);
    Task<(List<Product> Items, int TotalCount)> GetProductsPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<(List<Product> Items, int TotalCount)> SearchProductsAsync(
        string searchTerm,
        int page,
        int pageSize,
        Guid? categoryId = null,
        Guid? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default);
}
