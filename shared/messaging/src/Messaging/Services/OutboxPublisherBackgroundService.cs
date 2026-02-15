using CommonKernel.Contracts.Events;
using CommonKernel.Persistence;
using Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messaging.Services;

public abstract class OutboxPublisherBackgroundService<TDbContext> : BackgroundService
    where TDbContext : DbContext, IOutboxDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger _logger;

    protected OutboxPublisherBackgroundService(
        IServiceProvider serviceProvider,
        IRabbitMqPublisher publisher,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _publisher = publisher;
        _logger = logger;
    }

    protected virtual int BatchSize => 50;
    protected virtual TimeSpan PollInterval => TimeSpan.FromSeconds(2);
    protected abstract string SourceName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected outbox loop error in {SourceName}", SourceName);
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                var envelope = new EventEnvelope
                {
                    EventId = message.EventId,
                    EventType = message.EventType,
                    CorrelationId = message.CorrelationId,
                    Source = string.IsNullOrWhiteSpace(message.Source) ? SourceName : message.Source,
                    OccurredAtUtc = message.OccurredAtUtc,
                    Payload = message.Payload,
                    Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };

                await _publisher.PublishEnvelopeAsync(envelope, cancellationToken);

                message.PublishedAtUtc = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = exception.Message[..Math.Min(400, exception.Message.Length)];

                _logger.LogError(
                    exception,
                    "Failed to publish outbox message {OutboxMessageId} from {SourceName}. Attempt {Attempts}",
                    message.Id,
                    SourceName,
                    message.Attempts);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
