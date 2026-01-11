using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentAttemptEntity
{
    public Guid AttemptId { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Provider { get; private set; } = "PayTR";
    public string? ProviderReference { get; private set; }
    public PaymentAttemptStatus Status { get; private set; }
    public string? RequestPayload { get; private set; }
    public string? ResponsePayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Navigation
    public PaymentEntity? Payment { get; private set; }
    
    private PaymentAttemptEntity() { }
    
    public static PaymentAttemptEntity Create(Guid paymentId, string provider, string? requestPayload = null)
    {
        return new PaymentAttemptEntity
        {
            AttemptId = Guid.NewGuid(),
            PaymentId = paymentId,
            Provider = provider,
            Status = PaymentAttemptStatus.Initiated,
            RequestPayload = requestPayload,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void MarkAsWaitingCallback(string providerReference, string? responsePayload = null)
    {
        Status = PaymentAttemptStatus.WaitingCallback;
        ProviderReference = providerReference;
        ResponsePayload = responsePayload;
    }
    
    public void MarkAsSuccess(string? responsePayload = null)
    {
        Status = PaymentAttemptStatus.Success;
        ResponsePayload = responsePayload;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string errorMessage, string? responsePayload = null)
    {
        Status = PaymentAttemptStatus.Failed;
        ErrorMessage = errorMessage;
        ResponsePayload = responsePayload;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void MarkAsExpired()
    {
        Status = PaymentAttemptStatus.Expired;
        CompletedAt = DateTime.UtcNow;
    }
}

