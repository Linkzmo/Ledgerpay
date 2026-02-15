using System.Text.Json;
using CommonKernel.Contracts.Events;
using CommonKernel.Persistence;

namespace Risk.Worker.Infrastructure;

public static class OutboxFactory
{
    public static OutboxMessage Build(
        string eventType,
        object payload,
        string correlationId,
        string source)
    {
        return new OutboxMessage
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            CorrelationId = correlationId,
            Source = source,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}
