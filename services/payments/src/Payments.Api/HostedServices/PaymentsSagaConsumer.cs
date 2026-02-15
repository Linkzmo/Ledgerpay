using CommonKernel.Contracts.Events;
using Messaging;
using Messaging.Configuration;
using Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payments.Api.Domain;
using Payments.Api.Infrastructure;

namespace Payments.Api.HostedServices;

public sealed class PaymentsSagaConsumer : InboxConsumerBackgroundService<PaymentsDbContext>
{
    public PaymentsSagaConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<PaymentsSagaConsumer> logger)
        : base(serviceProvider, options, logger)
    {
    }

    protected override ConsumerTopology Topology => new(
        "payments.saga",
        "payments-saga",
        new[]
        {
            EventTypes.PaymentApproved,
            EventTypes.PaymentRejected,
            EventTypes.LedgerPosted
        });

    protected override async Task HandleMessageAsync(IServiceProvider serviceProvider, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<PaymentsDbContext>();

        switch (envelope.EventType)
        {
            case EventTypes.PaymentApproved:
            {
                var evt = envelope.DeserializePayload<PaymentApprovedEvent>();
                var payment = await dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == evt.PaymentId, cancellationToken);
                if (payment is null)
                {
                    return;
                }

                payment.MarkApproved(evt.DecisionReason);

                var existing = await dbContext.RiskAssessments.FirstOrDefaultAsync(x => x.PaymentId == evt.PaymentId, cancellationToken);
                if (existing is null)
                {
                    dbContext.RiskAssessments.Add(new RiskAssessment
                    {
                        PaymentId = evt.PaymentId,
                        Score = evt.Score,
                        Approved = true,
                        Reason = evt.DecisionReason,
                        EvaluatedAtUtc = evt.ApprovedAtUtc
                    });
                }

                break;
            }
            case EventTypes.PaymentRejected:
            {
                var evt = envelope.DeserializePayload<PaymentRejectedEvent>();
                var payment = await dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == evt.PaymentId, cancellationToken);
                if (payment is null)
                {
                    return;
                }

                payment.MarkRejected(evt.DecisionReason);

                var existing = await dbContext.RiskAssessments.FirstOrDefaultAsync(x => x.PaymentId == evt.PaymentId, cancellationToken);
                if (existing is null)
                {
                    dbContext.RiskAssessments.Add(new RiskAssessment
                    {
                        PaymentId = evt.PaymentId,
                        Score = evt.Score,
                        Approved = false,
                        Reason = evt.DecisionReason,
                        EvaluatedAtUtc = evt.RejectedAtUtc
                    });
                }

                break;
            }
            case EventTypes.LedgerPosted:
            {
                var evt = envelope.DeserializePayload<LedgerPostedEvent>();
                var payment = await dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == evt.PaymentId, cancellationToken);
                if (payment is null)
                {
                    return;
                }

                if (string.Equals(evt.Operation, "Post", StringComparison.OrdinalIgnoreCase))
                {
                    payment.MarkPosted();
                }
                else if (string.Equals(evt.Operation, "Reversal", StringComparison.OrdinalIgnoreCase))
                {
                    payment.MarkReversed("Reversal posted in ledger");
                }

                break;
            }
        }
    }
}
