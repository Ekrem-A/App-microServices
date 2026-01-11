using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IRefundRepository
{
    Task<RefundEntity?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundEntity>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRefundedAmountAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task AddAsync(RefundEntity refund, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefundEntity refund, CancellationToken cancellationToken = default);
}

