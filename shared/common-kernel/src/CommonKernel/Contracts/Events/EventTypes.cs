namespace CommonKernel.Contracts.Events;

public static class EventTypes
{
    public const string PaymentCreated = "payment.created.v1";
    public const string PaymentApproved = "payment.approved.v1";
    public const string PaymentRejected = "payment.rejected.v1";
    public const string LedgerPosted = "ledger.posted.v1";
    public const string PaymentReversed = "payment.reversed.v1";
}
