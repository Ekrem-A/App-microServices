using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentEntity?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<PaymentEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaymentEntity> AddAsync(PaymentEntity payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentEntity payment, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

