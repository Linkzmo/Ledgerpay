using CommonKernel.Contracts.Events;
using Ledger.Api.Domain;
using Ledger.Api.Infrastructure;
using Messaging;
using Messaging.Configuration;
using Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ledger.Api.HostedServices;

public sealed class LedgerEventsConsumer : InboxConsumerBackgroundService<LedgerDbContext>
{
    public LedgerEventsConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<LedgerEventsConsumer> logger)
        : base(serviceProvider, options, logger)
    {
    }

    protected override ConsumerTopology Topology => new(
        "ledger.events",
        "ledger-posting-worker",
        new[]
        {
            EventTypes.PaymentApproved,
            EventTypes.PaymentReversed
        });

    protected override async Task HandleMessageAsync(IServiceProvider serviceProvider, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<LedgerDbContext>();

        switch (envelope.EventType)
        {
            case EventTypes.PaymentApproved:
            {
                var evt = envelope.DeserializePayload<PaymentApprovedEvent>();
                var snapshot = await dbContext.PaymentSnapshots.FirstOrDefaultAsync(x => x.PaymentId == evt.PaymentId, cancellationToken);
                if (snapshot is null)
                {
                    snapshot = new PaymentLedgerSnapshot
                    {
                        PaymentId = evt.PaymentId,
                        Amount = evt.Amount,
                        Currency = evt.Currency,
                        IsPosted = false,
                        IsReversed = false,
                        LastUpdatedAtUtc = DateTimeOffset.UtcNow
                    };
                    dbContext.PaymentSnapshots.Add(snapshot);
                }

                if (snapshot.IsPosted)
                {
                    return;
                }

                dbContext.LedgerEntries.AddRange(
                    new LedgerEntry
                    {
                        PaymentId = evt.PaymentId,
                        Account = "CustomerCashAccount",
                        EntryType = LedgerEntryType.Debit,
                        Amount = evt.Amount,
                        Currency = evt.Currency,
                        Operation = "Post",
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    },
                    new LedgerEntry
                    {
                        PaymentId = evt.PaymentId,
                        Account = "MerchantSettlementAccount",
                        EntryType = LedgerEntryType.Credit,
                        Amount = evt.Amount,
                        Currency = evt.Currency,
                        Operation = "Post",
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });

                snapshot.IsPosted = true;
                snapshot.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

                dbContext.OutboxMessages.Add(OutboxFactory.Build(
                    EventTypes.LedgerPosted,
                    new LedgerPostedEvent(evt.PaymentId, "Post", DateTimeOffset.UtcNow),
                    envelope.CorrelationId));

                break;
            }
            case EventTypes.PaymentReversed:
            {
                var evt = envelope.DeserializePayload<PaymentReversedEvent>();
                var snapshot = await dbContext.PaymentSnapshots.FirstOrDefaultAsync(x => x.PaymentId == evt.PaymentId, cancellationToken);
                if (snapshot is null || !snapshot.IsPosted || snapshot.IsReversed)
                {
                    return;
                }

                dbContext.LedgerEntries.AddRange(
                    new LedgerEntry
                    {
                        PaymentId = evt.PaymentId,
                        Account = "MerchantSettlementAccount",
                        EntryType = LedgerEntryType.Debit,
                        Amount = evt.Amount,
                        Currency = evt.Currency,
                        Operation = "Reversal",
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    },
                    new LedgerEntry
                    {
                        PaymentId = evt.PaymentId,
                        Account = "CustomerCashAccount",
                        EntryType = LedgerEntryType.Credit,
                        Amount = evt.Amount,
                        Currency = evt.Currency,
                        Operation = "Reversal",
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });

                snapshot.IsReversed = true;
                snapshot.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

                dbContext.OutboxMessages.Add(OutboxFactory.Build(
                    EventTypes.LedgerPosted,
                    new LedgerPostedEvent(evt.PaymentId, "Reversal", DateTimeOffset.UtcNow),
                    envelope.CorrelationId));

                break;
            }
        }
    }
}
