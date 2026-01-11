using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Domain.Common;
using Order.Domain.Entities;
using System.Reflection;
using System.Text.Json;

namespace Order.Infrastructure.Persistence;

public class OrderDbContext : DbContext, IOrderDbContext
{
    public DbSet<Domain.Entities.Order> Orders => Set<Domain.Entities.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Convert domain events to outbox messages before saving
        var entities = ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = domainEvent.OccurredOnUtc
            };

            OutboxMessages.Add(outboxMessage);
        }

        // Clear domain events
        foreach (var entity in entities)
        {
            entity.Entity.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

