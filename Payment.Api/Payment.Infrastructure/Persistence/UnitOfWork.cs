using Microsoft.EntityFrameworkCore.Storage;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Persistence.Repositories;

namespace Payment.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly PaymentDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private IPaymentRepository? _payments;
    private IPaymentAttemptRepository? _paymentAttempts;
    private IOutboxRepository? _outboxMessages;
    private IRefundRepository? _refunds;

    public UnitOfWork(PaymentDbContext context)
    {
        _context = context;
    }

    public IPaymentRepository Payments => 
        _payments ??= new PaymentRepository(_context);

    public IPaymentAttemptRepository PaymentAttempts => 
        _paymentAttempts ??= new PaymentAttemptRepository(_context);

    public IOutboxRepository OutboxMessages => 
        _outboxMessages ??= new OutboxRepository(_context);

    public IRefundRepository Refunds => 
        _refunds ??= new RefundRepository(_context);

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
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
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

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

