namespace CommonKernel.Contracts.Events;

public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string PayerId,
    string MerchantId,
    DateTimeOffset CreatedAtUtc);

public sealed record PaymentApprovedEvent(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    int Score,
    string DecisionReason,
    DateTimeOffset ApprovedAtUtc);

public sealed record PaymentRejectedEvent(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    int Score,
    string DecisionReason,
    DateTimeOffset RejectedAtUtc);

public sealed record LedgerPostedEvent(
    Guid PaymentId,
    string Operation,
    DateTimeOffset PostedAtUtc);

public sealed record PaymentReversedEvent(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Reason,
    DateTimeOffset ReversedAtUtc);
