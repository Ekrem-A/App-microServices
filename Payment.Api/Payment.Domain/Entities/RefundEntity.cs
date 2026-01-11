using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class RefundEntity
{
    public Guid RefundId { get; private set; }
    public Guid PaymentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public RefundStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Navigation
    public PaymentEntity Payment { get; private set; } = null!;
    
    private RefundEntity() { }
    
    public static RefundEntity Create(Guid paymentId, decimal amount, string currency, string? reason = null)
    {
        return new RefundEntity
        {
            RefundId = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = amount,
            Currency = currency,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsProcessing()
    {
        Status = RefundStatus.Processing;
    }
    
    public void MarkAsCompleted(string providerReference)
    {
        Status = RefundStatus.Completed;
        ProviderReference = providerReference;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string reason)
    {
        Status = RefundStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void MarkAsRejected(string reason)
    {
        Status = RefundStatus.Rejected;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }
    
    public bool IsFinalState() => Status is RefundStatus.Completed or RefundStatus.Failed or RefundStatus.Rejected;
}

