using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentEntity
{
    public Guid PaymentId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public PaymentStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation
    public ICollection<PaymentAttemptEntity> Attempts { get; private set; } = new List<PaymentAttemptEntity>();
    public ICollection<RefundEntity> Refunds { get; private set; } = new List<RefundEntity>();
    
    private PaymentEntity() { }
    
    public static PaymentEntity Create(Guid orderId, Guid userId, decimal amount, string currency)
    {
        return new PaymentEntity
        {
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsProcessing()
    {
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsAuthorized(string providerReference)
    {
        Status = PaymentStatus.Authorized;
        ProviderReference = providerReference;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsPaid(string providerReference)
    {
        Status = PaymentStatus.Paid;
        ProviderReference = providerReference;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsRefunded()
    {
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanProcess() => Status == PaymentStatus.Pending;
    
    public bool IsFinalState() => Status is PaymentStatus.Paid or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Refunded;
}

