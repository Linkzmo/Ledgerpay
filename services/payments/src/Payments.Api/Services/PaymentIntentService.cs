using CommonKernel.Contracts.Events;
using CommonKernel.Payments;
using Microsoft.EntityFrameworkCore;
using Payments.Api.Contracts;
using Payments.Api.Domain;
using Payments.Api.Infrastructure;

namespace Payments.Api.Services;

public sealed class PaymentIntentService
{
    private readonly PaymentsDbContext _dbContext;
    private readonly IdempotencyCache _idempotencyCache;

    public PaymentIntentService(PaymentsDbContext dbContext, IdempotencyCache idempotencyCache)
    {
        _dbContext = dbContext;
        _idempotencyCache = idempotencyCache;
    }

    public async Task<(PaymentIntentResponse Response, bool IsNew, string? Error)> CreateAsync(
        CreatePaymentIntentRequest request,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var requestHash = PaymentMapping.ComputeRequestHash(request);

        var cached = await _idempotencyCache.GetAsync(idempotencyKey);
        if (cached is not null)
        {
            if (!string.Equals(cached.Value.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
            {
                return (default!, false, "Idempotency-Key already used with a different payload.");
            }

            var cachedPayment = await _dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == cached.Value.PaymentId, cancellationToken);
            if (cachedPayment is not null)
            {
                return (PaymentIntentResponse.FromEntity(cachedPayment), false, null);
            }
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
            {
                return (default!, false, "Idempotency-Key already used with a different payload.");
            }

            var existingPayment = await _dbContext.PaymentIntents.FirstAsync(x => x.Id == existing.PaymentId, cancellationToken);
            await _idempotencyCache.SetAsync(idempotencyKey, requestHash, existingPayment.Id, TimeSpan.FromHours(24));
            return (PaymentIntentResponse.FromEntity(existingPayment), false, null);
        }

        var payment = PaymentIntent.Create(
            request.Amount,
            request.Currency,
            request.PayerId,
            request.MerchantId,
            correlationId);

        _dbContext.PaymentIntents.Add(payment);
        _dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = idempotencyKey,
            RequestHash = requestHash,
            PaymentId = payment.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        _dbContext.OutboxMessages.Add(PaymentMapping.ToOutbox(
            EventTypes.PaymentCreated,
            new PaymentCreatedEvent(
                payment.Id,
                payment.Amount,
                payment.Currency,
                payment.PayerId,
                payment.MerchantId,
                payment.CreatedAtUtc),
            correlationId,
            "payments-api"));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await _idempotencyCache.SetAsync(idempotencyKey, requestHash, payment.Id, TimeSpan.FromHours(24));

        return (PaymentIntentResponse.FromEntity(payment), true, null);
    }

    public Task<PaymentIntent?> GetAsync(Guid paymentId, CancellationToken cancellationToken)
        => _dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

    public async Task<(bool Success, string Error)> RequestReversalAsync(
        Guid paymentId,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var payment = await _dbContext.PaymentIntents.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null)
        {
            return (false, "Payment not found.");
        }

        if (!payment.RequestReversal(reason))
        {
            return (false, $"Payment must be in {PaymentStatus.Posted} status to reverse.");
        }

        _dbContext.OutboxMessages.Add(PaymentMapping.ToOutbox(
            EventTypes.PaymentReversed,
            new PaymentReversedEvent(
                payment.Id,
                payment.Amount,
                payment.Currency,
                reason,
                DateTimeOffset.UtcNow),
            correlationId,
            "payments-api"));

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, string.Empty);
    }
}
