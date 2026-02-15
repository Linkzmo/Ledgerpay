namespace Ledger.Api.Contracts;

public sealed record LedgerEntryResponse(
    long Id,
    Guid PaymentId,
    string Account,
    string EntryType,
    decimal Amount,
    string Currency,
    string Operation,
    DateTimeOffset CreatedAtUtc);

public sealed record ReconciliationResponse(
    decimal PaymentsNetAmount,
    decimal LedgerNetAmount,
    decimal Difference,
    bool IsBalanced,
    DateTimeOffset GeneratedAtUtc);
