using System.Text;
using System.Text.Json;
using CommonKernel.Contracts.Events;
using CommonKernel.Persistence;
using Messaging.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messaging.Services;

public abstract class InboxConsumerBackgroundService<TDbContext> : BackgroundService
    where TDbContext : DbContext, IInboxDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;

    private IConnection? _connection;
    private IModel? _channel;

    protected InboxConsumerBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected abstract ConsumerTopology Topology { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, _options.PrefetchCount, global: false);

        ConfigureTopology(_channel);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) => await HandleDeliveryAsync(ea, stoppingToken);

        _channel.BasicConsume(queue: Topology.QueueName, autoAck: false, consumer);

        _logger.LogInformation("Consuming queue {QueueName} as {Consumer}", Topology.QueueName, Topology.ConsumerName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleDeliveryAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        EventEnvelope? envelope;

        try
        {
            var body = Encoding.UTF8.GetString(delivery.Body.Span);
            envelope = JsonSerializer.Deserialize<EventEnvelope>(body, EventEnvelopeExtensions.SerializerOptions());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Invalid event payload at queue {QueueName}", Topology.QueueName);
            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
            return;
        }

        if (envelope is null)
        {
            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var alreadyProcessed = await dbContext.InboxMessages.AnyAsync(
            x => x.EventId == envelope.EventId && x.Consumer == Topology.ConsumerName,
            cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Skipping duplicate EventId {EventId} for consumer {Consumer}",
                envelope.EventId,
                Topology.ConsumerName);

            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
            return;
        }

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["EventId"] = envelope.EventId,
            ["EventType"] = envelope.EventType
        });

        try
        {
            await HandleMessageAsync(scope.ServiceProvider, envelope, cancellationToken);

            dbContext.InboxMessages.Add(new InboxMessage
            {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Consumer = Topology.ConsumerName,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                ProcessedAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
        }
        catch (DbUpdateException exception) when (LooksLikeUniqueViolation(exception))
        {
            _logger.LogWarning(
                exception,
                "Duplicate inbox write for EventId {EventId} and consumer {Consumer}. Message acknowledged.",
                envelope.EventId,
                Topology.ConsumerName);

            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Consumer {Consumer} failed processing event {EventId} ({EventType})",
                Topology.ConsumerName,
                envelope.EventId,
                envelope.EventType);

            PublishRetryOrDlq(delivery, envelope, exception);
            _channel.BasicAck(delivery.DeliveryTag, multiple: false);
        }
    }

    private void PublishRetryOrDlq(BasicDeliverEventArgs delivery, EventEnvelope envelope, Exception exception)
    {
        if (_channel is null)
        {
            return;
        }

        var retryCount = 0;

        if (delivery.BasicProperties.Headers is not null
            && delivery.BasicProperties.Headers.TryGetValue("x-retry-count", out var raw)
            && TryParseHeaderAsInt(raw, out var parsed))
        {
            retryCount = parsed;
        }

        if (retryCount < _options.RetryLimit)
        {
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.CorrelationId = envelope.CorrelationId;
            properties.MessageId = envelope.EventId.ToString();
            properties.Headers = new Dictionary<string, object>
            {
                ["x-retry-count"] = (retryCount + 1).ToString(),
                ["x-last-error"] = exception.Message
            };

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: RetryQueueName(),
                mandatory: false,
                basicProperties: properties,
                body: delivery.Body);

            _logger.LogWarning(
                "Published retry {RetryCount} for EventId {EventId} to {RetryQueue}",
                retryCount + 1,
                envelope.EventId,
                RetryQueueName());

            return;
        }

        var dlqProps = _channel.CreateBasicProperties();
        dlqProps.Persistent = true;
        dlqProps.ContentType = "application/json";
        dlqProps.CorrelationId = envelope.CorrelationId;
        dlqProps.MessageId = envelope.EventId.ToString();
        dlqProps.Headers = new Dictionary<string, object>
        {
            ["x-final-error"] = exception.Message,
            ["x-retry-count"] = retryCount.ToString()
        };

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: DlqQueueName(),
            mandatory: false,
            basicProperties: dlqProps,
            body: delivery.Body);

        _logger.LogError(
            "Moved EventId {EventId} to DLQ {DlqQueue} after {RetryCount} retries",
            envelope.EventId,
            DlqQueueName(),
            retryCount);
    }

    private void ConfigureTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        channel.QueueDeclare(Topology.QueueName, durable: true, exclusive: false, autoDelete: false);

        foreach (var routingKey in Topology.RoutingKeys)
        {
            channel.QueueBind(Topology.QueueName, _options.ExchangeName, routingKey);
        }

        // Internal routing key used by retry queue DLX callback.
        channel.QueueBind(Topology.QueueName, _options.ExchangeName, Topology.QueueName);

        channel.QueueDeclare(
            queue: RetryQueueName(),
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _options.RetryDelayMs,
                ["x-dead-letter-exchange"] = _options.ExchangeName,
                ["x-dead-letter-routing-key"] = Topology.QueueName
            });

        channel.QueueDeclare(DlqQueueName(), durable: true, exclusive: false, autoDelete: false);
    }

    private string RetryQueueName() => $"{Topology.QueueName}.retry";
    private string DlqQueueName() => $"{Topology.QueueName}.dlq";

    private static bool TryParseHeaderAsInt(object raw, out int value)
    {
        switch (raw)
        {
            case byte[] bytes:
                return int.TryParse(Encoding.UTF8.GetString(bytes), out value);
            case string text:
                return int.TryParse(text, out value);
            case int number:
                value = number;
                return true;
            case long longNumber when longNumber is <= int.MaxValue and >= int.MinValue:
                value = (int)longNumber;
                return true;
            default:
                value = 0;
                return false;
        }
    }

    private static bool LooksLikeUniqueViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || message.Contains("unique", StringComparison.OrdinalIgnoreCase)
               || message.Contains("23505", StringComparison.OrdinalIgnoreCase);
    }

    protected abstract Task HandleMessageAsync(IServiceProvider serviceProvider, EventEnvelope envelope, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
