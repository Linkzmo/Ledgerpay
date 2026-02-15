using CommonKernel.Correlation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Contracts;
using Payments.Api.Security;
using Payments.Api.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentIntentService _service;

    public PaymentsController(PaymentIntentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentsWrite)]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ValidationProblem("Header Idempotency-Key is required.");
        }

        var correlationId = HttpContext.Items[CorrelationHeaders.CorrelationId]?.ToString() ?? Guid.NewGuid().ToString("N");

        var result = await _service.CreateAsync(request, idempotencyKey.ToString(), correlationId, cancellationToken);

        if (result.Error is not null)
        {
            return Conflict(new { message = result.Error });
        }

        if (!result.IsNew)
        {
            return Ok(result.Response);
        }

        return CreatedAtAction(nameof(GetById), new { paymentId = result.Response.Id }, result.Response);
    }

    [HttpGet("{paymentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsRead)]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _service.GetAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return NotFound();
        }

        return Ok(PaymentIntentResponse.FromEntity(payment));
    }

    [HttpPost("{paymentId:guid}/reverse")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsWrite)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reverse(
        Guid paymentId,
        [FromBody] ReversePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[CorrelationHeaders.CorrelationId]?.ToString() ?? Guid.NewGuid().ToString("N");

        var result = await _service.RequestReversalAsync(paymentId, request.Reason, correlationId, cancellationToken);

        if (!result.Success && result.Error == "Payment not found.")
        {
            return NotFound(new { message = result.Error });
        }

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Accepted();
    }
}
