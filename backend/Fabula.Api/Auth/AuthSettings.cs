using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fabula.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Fabula.Api.Auth;

public record JwtSettings(string Secret, string Issuer, string Audience, TimeSpan Lifetime)
{
    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Secret));
}

public static class RateLimits
{
    public const string Auth = "auth";
    public const string Ml = "ml";
}

public static class Policies
{
    public const string CanAuthor = "CanAuthor";
    public const string AdminOnly = "AdminOnly";
}

public class TokenService(JwtSettings settings)
{
    public string Create(User user)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        ];
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            expires: DateTime.UtcNow.Add(settings.Lifetime),
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(nameof(UserRole.Admin));
}
