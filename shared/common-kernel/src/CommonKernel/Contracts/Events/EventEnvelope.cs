namespace CommonKernel.Contracts.Events;

public sealed class EventEnvelope
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Payload { get; init; } = "{}";
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
