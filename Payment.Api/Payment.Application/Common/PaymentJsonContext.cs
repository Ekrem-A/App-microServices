using System.Text.Json.Serialization;
using Payment.Application.Events;

namespace Payment.Application.Common;

/// <summary>
/// Source-generated JSON serialization context for better performance and AOT compatibility.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(PaymentSucceededEvent))]
[JsonSerializable(typeof(PaymentFailedEvent))]
[JsonSerializable(typeof(RefundCompletedEvent))]
[JsonSerializable(typeof(RefundFailedEvent))]
[JsonSerializable(typeof(OrderCreatedEvent))]
public partial class PaymentJsonContext : JsonSerializerContext
{
}
