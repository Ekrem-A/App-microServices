using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Catalog.Infrastructure.QueryServices;
using Catalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Catalog.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;
    private readonly IProductQueryService _productQueryService;
    private readonly ICategoryQueryService _categoryQueryService;
    private readonly IBrandQueryService _brandQueryService;
    private IDbContextTransaction? _transaction;

    // Repositories - Lazy loading
    private IRepository<Product>? _products;
    private IRepository<Category>? _categories;
    private IRepository<Brand>? _brands;

    public UnitOfWork(
        CatalogDbContext context,
        IProductQueryService productQueryService,
        ICategoryQueryService categoryQueryService,
        IBrandQueryService brandQueryService)
    {
        _context = context;
        _productQueryService = productQueryService;
        _categoryQueryService = categoryQueryService;
        _brandQueryService = brandQueryService;
    }

    // ============================================
    // Repository Properties (Lazy Initialization)
    // ============================================

    public IRepository<Product> Products
    {
        get
        {
            _products ??= new Repository<Product>(_context);
            return _products;
        }
    }

    public IRepository<Category> Categories
    {
        get
        {
            _categories ??= new Repository<Category>(_context);
            return _categories;
        }
    }

    public IRepository<Brand> Brands
    {
        get
        {
            _brands ??= new Repository<Brand>(_context);
            return _brands;
        }
    }

    // ============================================
    // Query Service Properties (Injected via DI)
    // ============================================

    public IProductQueryService ProductQueries => _productQueryService;
    public ICategoryQueryService CategoryQueries => _categoryQueryService;
    public IBrandQueryService BrandQueries => _brandQueryService;

    // ============================================
    // Transaction Management
    // ============================================

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // ============================================
    // Dispose
    // ============================================

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}