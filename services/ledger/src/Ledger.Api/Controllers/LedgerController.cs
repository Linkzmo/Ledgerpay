using Ledger.Api.Contracts;
using Ledger.Api.Domain;
using Ledger.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ledger.Api.Security;

namespace Ledger.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class LedgerController : ControllerBase
{
    private readonly LedgerDbContext _dbContext;

    public LedgerController(LedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("ledger/payment/{paymentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.LedgerRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<LedgerEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntriesByPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.LedgerEntries
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.Id)
            .Select(x => new LedgerEntryResponse(
                x.Id,
                x.PaymentId,
                x.Account,
                x.EntryType.ToString(),
                x.Amount,
                x.Currency,
                x.Operation,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("reconciliation")]
    [Authorize(Policy = AuthorizationPolicies.LedgerRead)]
    [ProducesResponseType(typeof(ReconciliationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reconciliation(CancellationToken cancellationToken)
    {
        var posted = await _dbContext.PaymentSnapshots
            .Where(x => x.IsPosted)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var reversed = await _dbContext.PaymentSnapshots
            .Where(x => x.IsReversed)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var paymentsNet = posted - reversed;

        var customerCashDebits = await _dbContext.LedgerEntries
            .Where(x => x.Account == "CustomerCashAccount" && x.EntryType == LedgerEntryType.Debit)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var customerCashCredits = await _dbContext.LedgerEntries
            .Where(x => x.Account == "CustomerCashAccount" && x.EntryType == LedgerEntryType.Credit)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var ledgerNet = customerCashDebits - customerCashCredits;
        var diff = paymentsNet - ledgerNet;

        var response = new ReconciliationResponse(
            paymentsNet,
            ledgerNet,
            diff,
            Math.Abs(diff) < 0.0001m,
            DateTimeOffset.UtcNow);

        return Ok(response);
    }
}
