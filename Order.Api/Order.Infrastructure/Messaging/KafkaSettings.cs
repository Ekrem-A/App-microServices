namespace Order.Infrastructure.Messaging;

public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = "order-service";
    public string SecurityProtocol { get; set; } = "PLAINTEXT";
    public string SaslMechanism { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;

    // Topics - Publish
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string OrderCancelledTopic { get; set; } = "order-cancelled";

    // Topics - Consume
    public string PaymentSucceededTopic { get; set; } = "payment-succeeded";
    public string PaymentFailedTopic { get; set; } = "payment-failed";

    // Consumer settings
    public bool EnableAutoCommit { get; set; } = false;
    public string AutoOffsetReset { get; set; } = "earliest";
    public int SessionTimeoutMs { get; set; } = 45000;
    public int HeartbeatIntervalMs { get; set; } = 3000;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(BootstrapServers);
}
