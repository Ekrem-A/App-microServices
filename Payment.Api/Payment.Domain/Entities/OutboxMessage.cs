namespace Payment.Domain.Entities;

public class OutboxMessage
{
    public Guid MessageId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    
    private OutboxMessage() { }
    
    public static OutboxMessage Create(string type, string payload, string? correlationId = null)
    {
        return new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }
    
    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
    
    public void MarkAsFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
    
    public bool CanRetry(int maxRetries = 5) => RetryCount < maxRetries && ProcessedAt == null;
}

