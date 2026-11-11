using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PxOperations.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Emite tokens no formato v2 do Entra ID assinados com uma chave de teste. A
/// validação do API continua a de produção (issuer, audience, política); só a
/// chave de assinatura é trocada na factory, para não buscar metadata real.
/// </summary>
public static class TestTokens
{
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string ClientId = "22222222-2222-2222-2222-222222222222";
    public const string MemberAccount = "0";
    public const string GuestAccount = "1";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("px-operations-integration-tests-signing-key-0123456789"));

    public static string Issuer(string tenantId) => $"https://login.microsoftonline.com/{tenantId}/v2.0";

    public static string Member() => Create();

    public static string Create(
        string tenantId = TenantId,
        string audience = ClientId,
        string? account = MemberAccount)
    {
        var claims = new List<Claim>
        {
            new("tid", tenantId),
            new("name", "Pessoa BRQ"),
            new("preferred_username", "pessoa@brq.com")
        };
        if (account is not null)
        {
            claims.Add(new Claim("acct", account));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer(tenantId),
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}
