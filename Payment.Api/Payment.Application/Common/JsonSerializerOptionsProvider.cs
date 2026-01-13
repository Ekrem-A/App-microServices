using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payment.Application.Common;

/// <summary>
/// Provides cached JsonSerializerOptions instances to avoid repeated allocations.
/// </summary>
public static class JsonSerializerOptionsProvider
{
    /// <summary>
    /// Default options with case-insensitive property matching.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Options optimized for Kafka message serialization.
    /// </summary>
    public static JsonSerializerOptions Kafka { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
