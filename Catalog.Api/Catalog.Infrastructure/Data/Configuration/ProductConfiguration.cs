using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Data.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(550)
            .IsRequired();

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.BrandId)
            .HasColumnName("brand_id")
            .IsRequired();

        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id");

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.OriginalPrice)
            .HasColumnName("original_price")
            .HasColumnType("numeric(18,2)");

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.InStock)
            .HasColumnName("in_stock")
            .HasDefaultValue(true);

        builder.Property(p => p.StockQuantity)
            .HasColumnName("stock_quantity")
            .HasDefaultValue(0);

        builder.Property(p => p.Rating)
            .HasColumnName("rating")
            .HasColumnType("numeric(2,1)")
            .HasDefaultValue(0);

        builder.Property(p => p.ReviewCount)
            .HasColumnName("review_count")
            .HasDefaultValue(0);

        builder.Property(p => p.Images)
            .HasColumnName("images")
            .HasColumnType("text");

        builder.Property(p => p.Tags)
            .HasColumnName("tags")
            .HasColumnType("text");

        builder.Property(p => p.Featured)
            .HasColumnName("featured")
            .HasDefaultValue(false);

        builder.Property(p => p.IsCampaign)
            .HasColumnName("is_campaign")
            .HasDefaultValue(false);

        builder.Property(p => p.DiscountPercentage)
            .HasColumnName("discount_percentage")
            .HasDefaultValue(0);

        builder.Property(p => p.CampaignEndDate)
            .HasColumnName("campaign_end_date");

        builder.Property(p => p.ProductSource)
            .HasColumnName("product_source")
            .HasMaxLength(50)
            .HasDefaultValue("own");

        builder.Property(p => p.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}