using CommonKernel.Payments;

namespace Payments.Api.Domain;

public sealed class PaymentIntent
{
    private PaymentIntent()
    {
    }

    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string PayerId { get; private set; } = string.Empty;
    public string MerchantId { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? LastReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PaymentIntent Create(
        decimal amount,
        string currency,
        string payerId,
        string merchantId,
        string correlationId)
    {
        var now = DateTimeOffset.UtcNow;

        return new PaymentIntent
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            PayerId = payerId,
            MerchantId = merchantId,
            CorrelationId = correlationId,
            Status = PaymentStatus.PendingRisk,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void MarkApproved(string reason)
    {
        if (Status != PaymentStatus.PendingRisk)
        {
            return;
        }

        Status = PaymentStatus.Approved;
        LastReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRejected(string reason)
    {
        if (Status != PaymentStatus.PendingRisk)
        {
            return;
        }

        Status = PaymentStatus.Rejected;
        LastReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkPosted()
    {
        if (Status != PaymentStatus.Approved && Status != PaymentStatus.ReversalRequested)
        {
            return;
        }

        Status = PaymentStatus.Posted;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool RequestReversal(string reason)
    {
        if (Status != PaymentStatus.Posted)
        {
            return false;
        }

        Status = PaymentStatus.ReversalRequested;
        LastReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public void MarkReversed(string reason)
    {
        if (Status != PaymentStatus.ReversalRequested && Status != PaymentStatus.Posted)
        {
            return;
        }

        Status = PaymentStatus.Reversed;
        LastReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
