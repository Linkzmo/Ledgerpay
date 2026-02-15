using System.Diagnostics;

namespace CommonKernel.Correlation;

public static class CorrelationContext
{
    public static string GetOrCreate(string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            return incoming;
        }

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
