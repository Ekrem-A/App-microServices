using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentAttemptRepository
{
    Task<PaymentAttemptEntity?> GetByIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<PaymentAttemptEntity?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default);
    Task<PaymentAttemptEntity> AddAsync(PaymentAttemptEntity attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentAttemptEntity attempt, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentAttemptEntity>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
}

