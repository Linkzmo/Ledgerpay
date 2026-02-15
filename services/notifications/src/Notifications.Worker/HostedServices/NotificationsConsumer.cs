using CommonKernel.Contracts.Events;
using Messaging;
using Messaging.Configuration;
using Messaging.Services;
using Microsoft.Extensions.Options;
using Notifications.Worker.Domain;
using Notifications.Worker.Infrastructure;

namespace Notifications.Worker.HostedServices;

public sealed class NotificationsConsumer : InboxConsumerBackgroundService<NotificationsDbContext>
{
    public NotificationsConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<NotificationsConsumer> logger)
        : base(serviceProvider, options, logger)
    {
    }

    protected override ConsumerTopology Topology => new(
        "notifications.events",
        "notifications-worker",
        new[]
        {
            EventTypes.PaymentRejected,
            EventTypes.LedgerPosted
        });

    protected override Task HandleMessageAsync(IServiceProvider serviceProvider, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<NotificationsDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<NotificationsConsumer>>();

        string message;
        Guid paymentId;

        if (envelope.EventType == EventTypes.PaymentRejected)
        {
            var evt = envelope.DeserializePayload<PaymentRejectedEvent>();
            paymentId = evt.PaymentId;
            message = $"Payment {evt.PaymentId} was rejected. Reason: {evt.DecisionReason}";
        }
        else
        {
            var evt = envelope.DeserializePayload<LedgerPostedEvent>();
            paymentId = evt.PaymentId;
            message = $"Payment {evt.PaymentId} ledger operation completed: {evt.Operation}.";
        }

        dbContext.Notifications.Add(new NotificationLog
        {
            EventId = envelope.EventId,
            PaymentId = paymentId,
            Channel = "webhook",
            Destination = "https://webhook.site/fake-ledgerpay",
            Message = message,
            SentAtUtc = DateTimeOffset.UtcNow
        });

        logger.LogInformation("Fake notification sent for PaymentId {PaymentId}: {Message}", paymentId, message);
        return Task.CompletedTask;
    }
}
