using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Standard versioned event envelope used for all Kafka domain events in FPT Alumni Connect.
/// </summary>
/// <typeparam name="T">Type of the event payload.</typeparam>
public class EventEnvelope<T>
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("producer")]
    public string Producer { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    public EventEnvelope()
    {
    }

    public EventEnvelope(string eventType, string version, string producer, T data, string? correlationId = null)
    {
        EventType = eventType;
        Version = version;
        Producer = producer;
        Data = data;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        OccurredAt = DateTime.UtcNow;
    }
}
