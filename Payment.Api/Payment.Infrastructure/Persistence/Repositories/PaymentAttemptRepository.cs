using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;  

namespace Payment.Infrastructure.Persistence.Repositories;

public class PaymentAttemptRepository : IPaymentAttemptRepository
{
    private readonly PaymentDbContext _context;

    public PaymentAttemptRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentAttemptEntity?> GetByIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentAttempts
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId, cancellationToken);
    }

    public async Task<PaymentAttemptEntity?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ProviderReference == providerReference, cancellationToken);
    }

    public async Task<PaymentAttemptEntity> AddAsync(PaymentAttemptEntity attempt, CancellationToken cancellationToken = default)
    {
        await _context.PaymentAttempts.AddAsync(attempt, cancellationToken);
        return attempt;
    }

    public Task UpdateAsync(PaymentAttemptEntity attempt, CancellationToken cancellationToken = default)
    {
        _context.PaymentAttempts.Update(attempt);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<PaymentAttemptEntity>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentAttempts
            .Where(a => a.PaymentId == paymentId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

