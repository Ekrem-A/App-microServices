namespace Order.Application.Common.IntegrationEvents;

/// <summary>
/// Consumed from Payment service when payment succeeds.
/// Order service marks order as payment completed + processing.
/// </summary>
public record PaymentSucceededIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string ProviderReference { get; init; } = string.Empty;
    public decimal PaidAmount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime PaidAt { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Consumed from Payment service when payment fails.
/// Order service marks order as failed.
/// </summary>
public record PaymentFailedIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonMessage { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
    public string? CorrelationId { get; init; }
}
