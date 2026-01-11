using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.ProductImageUrl)
            .HasMaxLength(500);

        builder.Property(i => i.Quantity)
            .IsRequired();

        // Money value objects
        builder.OwnsOne(i => i.UnitPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2).IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(i => i.TotalPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("TotalPriceAmount").HasPrecision(18, 2).IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("TotalPriceCurrency").HasMaxLength(3).IsRequired();
        });

        // Index for common queries
        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.ProductId);

        // Ignore domain events
        builder.Ignore(i => i.DomainEvents);
    }
}

