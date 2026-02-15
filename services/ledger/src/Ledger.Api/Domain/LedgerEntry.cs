namespace Ledger.Api.Domain;

public sealed class LedgerEntry
{
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public string Account { get; set; } = string.Empty;
    public LedgerEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
