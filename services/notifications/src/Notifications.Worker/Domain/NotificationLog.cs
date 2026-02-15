namespace Notifications.Worker.Domain;

public sealed class NotificationLog
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public Guid PaymentId { get; set; }
    public string Channel { get; set; } = "email";
    public string Destination { get; set; } = "merchant@example.com";
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
