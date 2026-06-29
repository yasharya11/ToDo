namespace ToDoApi.Auth;

/// <summary>
/// JWT settings bound from the "Jwt" configuration section. The signing key shipped in
/// appsettings.json is a development value so a fresh clone runs with no setup; a real
/// deployment overrides it (and the issuer/audience) via environment variables or a secret
/// store (see README). HS256 requires the key to be at least 32 bytes — validated at startup.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer; validated on every incoming token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Intended token audience; validated on every incoming token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key (HS256). Dev value in config; override in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes. Short-lived: there is no refresh token.</summary>
    public int AccessTokenMinutes { get; set; } = 60;
}
