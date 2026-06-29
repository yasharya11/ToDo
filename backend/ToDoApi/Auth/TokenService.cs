using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ToDoApi.Models;

namespace ToDoApi.Auth;

/// <summary>
/// Issues short-lived JWT access tokens for authenticated users. Symmetric HS256 signing
/// with the configured key. The token's subject (<c>sub</c>) claim carries the user id that
/// the task endpoints scope ownership to once <c>[Authorize]</c> lands (issue #18).
/// </summary>
public class TokenService
{
    // JsonWebTokenHandler is thread-safe and stateless, so a single shared instance is fine.
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(JwtOptions options)
    {
        _options = options;
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Creates a signed access token for the user, expiring after the configured lifetime.
    /// </summary>
    public string CreateAccessToken(User user)
    {
        DateTime now = DateTime.UtcNow;
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_options.AccessTokenMinutes),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = _signingCredentials,
        };

        return Handler.CreateToken(descriptor);
    }
}
