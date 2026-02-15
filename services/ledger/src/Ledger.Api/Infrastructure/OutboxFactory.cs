using System.Text.Json;
using CommonKernel.Persistence;

namespace Ledger.Api.Infrastructure;

public static class OutboxFactory
{
    public static OutboxMessage Build(string eventType, object payload, string correlationId)
    {
        return new OutboxMessage
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            CorrelationId = correlationId,
            Source = "ledger-api",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}
