using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly PaymentDbContext _context;

    public RefundRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<RefundEntity?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.RefundId == refundId, cancellationToken);
    }

    public async Task<IEnumerable<RefundEntity>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.PaymentId == paymentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalRefundedAmountAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.PaymentId == paymentId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount, cancellationToken);
    }

    public async Task AddAsync(RefundEntity refund, CancellationToken cancellationToken = default)
    {
        await _context.Refunds.AddAsync(refund, cancellationToken);
    }

    public Task UpdateAsync(RefundEntity refund, CancellationToken cancellationToken = default)
    {
        _context.Refunds.Update(refund);
        return Task.CompletedTask;
    }
}

