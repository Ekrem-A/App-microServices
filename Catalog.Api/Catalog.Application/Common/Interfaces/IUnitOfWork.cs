using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;

namespace Catalog.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Brand> Brands { get; }

    // Query Services
    IProductQueryService ProductQueries { get; }
    ICategoryQueryService CategoryQueries { get; }
    IBrandQueryService BrandQueries { get; }

    // Transaction Management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
