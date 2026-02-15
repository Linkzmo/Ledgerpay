namespace Messaging.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "ledgerpay.events";
    public ushort PrefetchCount { get; set; } = 10;
    public int RetryLimit { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 5000;
}
