using Messaging.Abstractions;
using Messaging.Services;
using Payments.Api.Infrastructure;

namespace Payments.Api.HostedServices;

public sealed class PaymentsOutboxPublisher : OutboxPublisherBackgroundService<PaymentsDbContext>
{
    public PaymentsOutboxPublisher(
        IServiceProvider serviceProvider,
        IRabbitMqPublisher publisher,
        ILogger<PaymentsOutboxPublisher> logger)
        : base(serviceProvider, publisher, logger)
    {
    }

    protected override string SourceName => "payments-api";
}
