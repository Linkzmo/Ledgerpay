namespace CommonKernel.Payments;

public enum PaymentStatus
{
    PendingRisk = 0,
    Approved = 1,
    Rejected = 2,
    Posted = 3,
    ReversalRequested = 4,
    Reversed = 5
}
