using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.QueryServices;

public class ProductQueryService : IProductQueryService
{
    private readonly CatalogDbContext _context;

    public ProductQueryService(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetProductWithIncludesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetProductByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Product>> GetAllProductsWithIncludesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetProductsPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    public async Task<(List<Product> Items, int TotalCount)> SearchProductsAsync(
        string searchTerm,
        int page,
        int pageSize,
        Guid? categoryId = null,
        Guid? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var search = searchTerm.ToLower().Trim();

        var query = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)) ||
                p.Brand.Name.ToLower().Contains(search) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(search)));

        // Apply filters
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }
}
