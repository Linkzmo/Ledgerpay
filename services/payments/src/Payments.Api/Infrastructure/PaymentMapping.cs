using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommonKernel.Persistence;
using Payments.Api.Contracts;

namespace Payments.Api.Infrastructure;

public static class PaymentMapping
{
    public static string ComputeRequestHash(CreatePaymentIntentRequest request)
    {
        var canonical = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    public static OutboxMessage ToOutbox(
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
