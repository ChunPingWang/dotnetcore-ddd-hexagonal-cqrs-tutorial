using System.Security.Claims;

namespace BankAccountQuery.Infrastructure.Adapters.In.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 自 JWT Claim 萃取 CustomerId（優先 "customer_id"，其次 NameIdentifier）。
    /// </summary>
    public static string GetCustomerId(this ClaimsPrincipal user)
    {
        var customerId =
            user.FindFirstValue("customer_id")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(customerId))
            throw new UnauthorizedAccessException("JWT 未包含 customer_id Claim");

        return customerId;
    }
}
