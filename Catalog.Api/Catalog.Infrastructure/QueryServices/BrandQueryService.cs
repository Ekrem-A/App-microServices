using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.QueryServices;

public class BrandQueryService : IBrandQueryService
{
    private readonly CatalogDbContext _context;

    public BrandQueryService(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Brand?> GetBrandWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Brands
            .Include(b => b.Products)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<List<Brand>> GetAllBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Brands
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Brands
            .Where(b => b.IsActive && b.Products.Any(p => p.InStock))
            .OrderBy(b => b.DisplayOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }
}
