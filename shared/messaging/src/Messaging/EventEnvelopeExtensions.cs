using System.Text.Json;
using CommonKernel.Contracts.Events;

namespace Messaging;

public static class EventEnvelopeExtensions
{
    public static TPayload DeserializePayload<TPayload>(this EventEnvelope envelope)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(envelope.Payload, SerializerOptions());
        if (payload is null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload to {typeof(TPayload).Name}.");
        }

        return payload;
    }

    public static JsonSerializerOptions SerializerOptions() => new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
