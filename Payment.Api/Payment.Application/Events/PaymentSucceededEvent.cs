namespace Payment.Application.Events;

/// <summary>
/// Event published when payment is successful.
/// Order service consumes this to mark order as paid.
/// </summary>
public class PaymentSucceededEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime PaidAt { get; set; }
    public string? CorrelationId { get; set; }
}

