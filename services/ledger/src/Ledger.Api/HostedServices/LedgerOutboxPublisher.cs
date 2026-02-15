using Messaging.Abstractions;
using Messaging.Services;
using Ledger.Api.Infrastructure;

namespace Ledger.Api.HostedServices;

public sealed class LedgerOutboxPublisher : OutboxPublisherBackgroundService<LedgerDbContext>
{
    public LedgerOutboxPublisher(
        IServiceProvider serviceProvider,
        IRabbitMqPublisher publisher,
        ILogger<LedgerOutboxPublisher> logger)
        : base(serviceProvider, publisher, logger)
    {
    }

    protected override string SourceName => "ledger-api";
}
