namespace Payment.Application.Events;

/// <summary>
/// Event published when payment fails.
/// Order service consumes this to cancel or revert the order.
/// </summary>
public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
    public string? CorrelationId { get; set; }
}

