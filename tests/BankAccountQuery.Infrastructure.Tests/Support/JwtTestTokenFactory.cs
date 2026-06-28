using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BankAccountQuery.Infrastructure.Tests.Support;

/// <summary>
/// 產生測試用 JWT（HS256），與 appsettings.json 的 Jwt 設定一致。
/// </summary>
public static class JwtTestTokenFactory
{
    public const string SigningKey = "dev-only-super-secret-signing-key-please-change-32+chars";
    public const string Issuer = "BankAccountQuery";
    public const string Audience = "BankAccountQuery.Clients";

    public static string ForCustomer(string customerId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim("customer_id", customerId) },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
