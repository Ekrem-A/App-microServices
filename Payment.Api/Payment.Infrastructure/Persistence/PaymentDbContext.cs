using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<PaymentAttemptEntity> PaymentAttempts => Set<PaymentAttemptEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<RefundEntity> Refunds => Set<RefundEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Payment Entity Configuration
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.ToTable("payments");
            
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            
            entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired();
            entity.HasIndex(e => e.OrderId).IsUnique();
            
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference").HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasMany(e => e.Attempts)
                .WithOne(a => a.Payment)
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Refunds)
                .WithOne(r => r.Payment)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment Attempt Entity Configuration
        modelBuilder.Entity<PaymentAttemptEntity>(entity =>
        {
            entity.ToTable("payment_attempts");
            
            entity.HasKey(e => e.AttemptId);
            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            
            entity.Property(e => e.PaymentId).HasColumnName("payment_id").IsRequired();
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference").HasMaxLength(500);
            entity.HasIndex(e => e.ProviderReference);
            
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.RequestPayload).HasColumnName("request_payload").HasColumnType("text");
            entity.Property(e => e.ResponsePayload).HasColumnName("response_payload").HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
        });

        // Outbox Message Configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("text").IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count").IsRequired();
            entity.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(2000);

            // Index for fetching unprocessed messages
            entity.HasIndex(e => new { e.ProcessedAt, e.OccurredAt })
                .HasDatabaseName("ix_outbox_pending");
        });

        // Refund Entity Configuration
        modelBuilder.Entity<RefundEntity>(entity =>
        {
            entity.ToTable("refunds");
            
            entity.HasKey(e => e.RefundId);
            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            
            entity.Property(e => e.PaymentId).HasColumnName("payment_id").IsRequired();
            entity.HasIndex(e => e.PaymentId);
            
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference").HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
        });
    }
}

