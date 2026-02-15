using CommonKernel.Payments;
using Payments.Api.Domain;

namespace Payments.Api.Contracts;

public sealed record CreatePaymentIntentRequest(decimal Amount, string Currency, string PayerId, string MerchantId);

public sealed record ReversePaymentRequest(string Reason);

public sealed record PaymentIntentResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string PayerId,
    string MerchantId,
    PaymentStatus Status,
    string? LastReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static PaymentIntentResponse FromEntity(PaymentIntent intent) => new(
        intent.Id,
        intent.Amount,
        intent.Currency,
        intent.PayerId,
        intent.MerchantId,
        intent.Status,
        intent.LastReason,
        intent.CreatedAtUtc,
        intent.UpdatedAtUtc);
}
