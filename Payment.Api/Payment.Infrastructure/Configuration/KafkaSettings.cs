namespace Payment.Infrastructure.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "payment-service";
    public string SecurityProtocol { get; set; } = "PLAINTEXT";
    public string SaslMechanism { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    
    // Topics
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string PaymentSucceededTopic { get; set; } = "payment-succeeded";
    public string PaymentFailedTopic { get; set; } = "payment-failed";
    public string RefundCompletedTopic { get; set; } = "refund-completed";
    public string RefundFailedTopic { get; set; } = "refund-failed";
    
    // Consumer settings
    public bool EnableAutoCommit { get; set; } = false;
    public string AutoOffsetReset { get; set; } = "earliest";
    public int SessionTimeoutMs { get; set; } = 45000;
    public int HeartbeatIntervalMs { get; set; } = 3000;
}

