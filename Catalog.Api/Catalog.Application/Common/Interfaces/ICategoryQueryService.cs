using Catalog.Domain.Entities;

namespace Catalog.Application.Common.Interfaces;

public interface ICategoryQueryService
{
    Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> GetCategoriesWithFilteringAsync(bool? isActive, Guid? parentId, CancellationToken cancellationToken = default);
}
