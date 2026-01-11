using Microsoft.EntityFrameworkCore;

namespace Order.Application.Common.Interfaces;

public interface IOrderDbContext
{
    DbSet<Domain.Entities.Order> Orders { get; }
    DbSet<Domain.Entities.OrderItem> OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

