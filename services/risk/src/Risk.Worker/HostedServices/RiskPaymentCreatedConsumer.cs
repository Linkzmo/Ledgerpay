using CommonKernel.Contracts.Events;
using Messaging;
using Messaging.Configuration;
using Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Risk.Worker.Domain;
using Risk.Worker.Infrastructure;
using Risk.Worker.Services;

namespace Risk.Worker.HostedServices;

public sealed class RiskPaymentCreatedConsumer : InboxConsumerBackgroundService<RiskDbContext>
{
    public RiskPaymentCreatedConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RiskPaymentCreatedConsumer> logger)
        : base(serviceProvider, options, logger)
    {
    }

    protected override ConsumerTopology Topology => new(
        "risk.payment-created",
        "risk-worker",
        new[] { EventTypes.PaymentCreated });

    protected override async Task HandleMessageAsync(IServiceProvider serviceProvider, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<RiskDbContext>();
        var decisionService = serviceProvider.GetRequiredService<RiskDecisionService>();

        var evt = envelope.DeserializePayload<PaymentCreatedEvent>();

        var exists = await dbContext.RiskAssessments.AnyAsync(x => x.PaymentId == evt.PaymentId, cancellationToken);
        if (exists)
        {
            return;
        }

        var decision = decisionService.Evaluate(evt);

        dbContext.RiskAssessments.Add(new RiskAssessmentRecord
        {
            PaymentId = evt.PaymentId,
            Amount = evt.Amount,
            Currency = evt.Currency,
            Score = decision.Score,
            Approved = decision.Approved,
            Reason = decision.Reason,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        });

        if (decision.Approved)
        {
            dbContext.OutboxMessages.Add(OutboxFactory.Build(
                EventTypes.PaymentApproved,
                new PaymentApprovedEvent(
                    evt.PaymentId,
                    evt.Amount,
                    evt.Currency,
                    decision.Score,
                    decision.Reason,
                    DateTimeOffset.UtcNow),
                envelope.CorrelationId,
                "risk-worker"));
        }
        else
        {
            dbContext.OutboxMessages.Add(OutboxFactory.Build(
                EventTypes.PaymentRejected,
                new PaymentRejectedEvent(
                    evt.PaymentId,
                    evt.Amount,
                    evt.Currency,
                    decision.Score,
                    decision.Reason,
                    DateTimeOffset.UtcNow),
                envelope.CorrelationId,
                "risk-worker"));
        }

    }
}
