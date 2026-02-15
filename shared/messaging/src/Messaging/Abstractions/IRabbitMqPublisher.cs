using CommonKernel.Contracts.Events;

namespace Messaging.Abstractions;

public interface IRabbitMqPublisher
{
    Task PublishAsync<TPayload>(
        string eventType,
        TPayload payload,
        string correlationId,
        string source,
        Guid? eventId = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
