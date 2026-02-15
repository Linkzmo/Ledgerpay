namespace Messaging.Configuration;

public sealed record ConsumerTopology(string QueueName, string ConsumerName, IReadOnlyCollection<string> RoutingKeys);
