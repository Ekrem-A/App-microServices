using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.QueryServices;

public class CategoryQueryService : ICategoryQueryService
{
    private readonly CatalogDbContext _context;

    public CategoryQueryService(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Products)
                .ThenInclude(p => p.Brand)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Parent)
            .Where(c => c.Products.Any(p => p.InStock))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Category>> GetCategoriesWithFilteringAsync(bool? isActive, Guid? parentId, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .Include(c => c.Parent)
            .AsQueryable();

        if (isActive.HasValue)
        {
            if (isActive.Value)
                query = query.Where(c => c.Products.Any(p => p.InStock));
        }

        if (parentId.HasValue)
            query = query.Where(c => c.ParentId == parentId.Value);

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
