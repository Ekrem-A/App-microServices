namespace Payment.Application.Events;

public class RefundCompletedEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid RefundId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RefundedAt { get; set; }
    public string? CorrelationId { get; set; }
}

public class RefundFailedEvent
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid RefundId { get; set; }
    public decimal RefundAmount { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
    public string? CorrelationId { get; set; }
}

