using Messaging.Abstractions;
using Messaging.Services;
using Risk.Worker.Infrastructure;

namespace Risk.Worker.HostedServices;

public sealed class RiskOutboxPublisher : OutboxPublisherBackgroundService<RiskDbContext>
{
    public RiskOutboxPublisher(
        IServiceProvider serviceProvider,
        IRabbitMqPublisher publisher,
        ILogger<RiskOutboxPublisher> logger)
        : base(serviceProvider, publisher, logger)
    {
    }

    protected override string SourceName => "risk-worker";
}
