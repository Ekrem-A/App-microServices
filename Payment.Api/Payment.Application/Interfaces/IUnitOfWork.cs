namespace Payment.Application.Interfaces;

public interface IUnitOfWork
{
    IPaymentRepository Payments { get; }
    IPaymentAttemptRepository PaymentAttempts { get; }
    IOutboxRepository OutboxMessages { get; }
    IRefundRepository Refunds { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

