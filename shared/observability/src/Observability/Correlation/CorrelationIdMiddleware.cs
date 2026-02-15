using CommonKernel.Correlation;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Observability.Correlation;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = CorrelationContext.GetOrCreate(context.Request.Headers[CorrelationHeaders.CorrelationId].FirstOrDefault());

        context.Items[CorrelationHeaders.CorrelationId] = correlationId;
        context.Response.Headers[CorrelationHeaders.CorrelationId] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
