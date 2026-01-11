namespace Cart.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public KafkaTopics Topics { get; set; } = new();
}

public class KafkaTopics
{
    public string CheckoutRequested { get; set; } = "cart.checkout.v1";
}

