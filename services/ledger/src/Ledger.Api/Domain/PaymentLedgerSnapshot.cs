namespace Ledger.Api.Domain;

public sealed class PaymentLedgerSnapshot
{
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsPosted { get; set; }
    public bool IsReversed { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
