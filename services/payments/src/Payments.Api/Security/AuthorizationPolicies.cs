using Microsoft.AspNetCore.Authorization;

namespace Payments.Api.Security;

public static class AuthorizationPolicies
{
    public const string PaymentsRead = "payments.read";
    public const string PaymentsWrite = "payments.write";

    public static void AddPaymentPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(PaymentsRead, policy => policy.RequireAssertion(context => HasScope(context.User, PaymentsRead)));
        options.AddPolicy(PaymentsWrite, policy => policy.RequireAssertion(context => HasScope(context.User, PaymentsWrite)));
    }

    private static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string scope)
    {
        return user.Claims
            .Where(x => x.Type == "scope")
            .SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(x => string.Equals(x, scope, StringComparison.OrdinalIgnoreCase));
    }
}
