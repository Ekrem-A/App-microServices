using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.ValueObjects;

namespace Order.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Domain.Entities.Order>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.IdempotencyKey)
            .HasMaxLength(100);

        builder.HasIndex(o => new { o.IdempotencyKey, o.UserId })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Property(o => o.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        // Shipping Address (owned entity)
        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200).IsRequired();
            addressBuilder.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.District).HasColumnName("ShippingDistrict").HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20).IsRequired();
            addressBuilder.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.BuildingNumber).HasColumnName("ShippingBuildingNumber").HasMaxLength(20);
            addressBuilder.Property(a => a.ApartmentNumber).HasColumnName("ShippingApartmentNumber").HasMaxLength(20);
        });

        // Billing Address (owned entity, optional)
        builder.OwnsOne(o => o.BillingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasColumnName("BillingStreet").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("BillingCity").HasMaxLength(100);
            addressBuilder.Property(a => a.District).HasColumnName("BillingDistrict").HasMaxLength(100);
            addressBuilder.Property(a => a.PostalCode).HasColumnName("BillingPostalCode").HasMaxLength(20);
            addressBuilder.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(100);
            addressBuilder.Property(a => a.BuildingNumber).HasColumnName("BillingBuildingNumber").HasMaxLength(20);
            addressBuilder.Property(a => a.ApartmentNumber).HasColumnName("BillingApartmentNumber").HasMaxLength(20);
        });

        // Money value objects
        builder.OwnsOne(o => o.SubTotal, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("SubTotalAmount").HasPrecision(18, 2).IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("SubTotalCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(o => o.ShippingCost, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("ShippingCostAmount").HasPrecision(18, 2).IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("ShippingCostCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(o => o.TotalAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2).IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("TotalCurrency").HasMaxLength(3).IsRequired();
        });

        // Navigation to items
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAtUtc);

        // Ignore domain events (they are not persisted directly)
        builder.Ignore(o => o.DomainEvents);
    }
}

