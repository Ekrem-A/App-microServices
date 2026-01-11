using Catalog.Domain.Entities;

namespace Catalog.Application.Common.Interfaces;

public interface IBrandQueryService
{
    Task<Brand?> GetBrandWithProductsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Brand>> GetAllBrandsAsync(CancellationToken cancellationToken = default);
    Task<List<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default);
}
