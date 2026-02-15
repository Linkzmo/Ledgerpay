using System.Text;
using System.Text.Json;
using CommonKernel.Contracts.Events;
using Messaging.Abstractions;
using Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Messaging.RabbitMq;

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync<TPayload>(
        string eventType,
        TPayload payload,
        string correlationId,
        string source,
        Guid? eventId = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var envelope = new EventEnvelope
        {
            EventId = eventId ?? Guid.NewGuid(),
            EventType = eventType,
            CorrelationId = correlationId,
            Source = source,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.Serialize(payload, EventEnvelopeExtensions.SerializerOptions()),
            Headers = headers is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
        };

        return PublishEnvelopeAsync(envelope, cancellationToken);
    }

    public Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = GetConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, EventEnvelopeExtensions.SerializerOptions()));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.CorrelationId = envelope.CorrelationId;
        properties.MessageId = envelope.EventId.ToString();
        properties.Timestamp = new AmqpTimestamp(envelope.OccurredAtUtc.ToUnixTimeSeconds());

        if (envelope.Headers.Count > 0)
        {
            properties.Headers = envelope.Headers.ToDictionary(
                x => x.Key,
                x => (object)Encoding.UTF8.GetBytes(x.Value));
        }

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: envelope.EventType,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published event {EventType} with EventId {EventId} and CorrelationId {CorrelationId}",
            envelope.EventType,
            envelope.EventId,
            envelope.CorrelationId);

        return Task.CompletedTask;
    }

    private IConnection GetConnection()
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        lock (_sync)
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
