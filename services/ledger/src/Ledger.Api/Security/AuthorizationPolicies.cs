using Microsoft.AspNetCore.Authorization;

namespace Ledger.Api.Security;

public static class AuthorizationPolicies
{
    public const string LedgerRead = "ledger.read";

    public static void AddLedgerPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(LedgerRead, policy => policy.RequireAssertion(context => HasScope(context.User, LedgerRead)));
    }

    private static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string scope)
    {
        return user.Claims
            .Where(x => x.Type == "scope")
            .SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(x => string.Equals(x, scope, StringComparison.OrdinalIgnoreCase));
    }
}
