using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Content)
            .IsRequired();

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.HasIndex(o => o.ProcessedOnUtc)
            .HasFilter("\"ProcessedOnUtc\" IS NULL");

        builder.HasIndex(o => o.OccurredOnUtc);
    }
}

