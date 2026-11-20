namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// JWT and refresh token configuration options.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = "ClinicBook";

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = "ClinicBook.Api";

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>Symmetric signing key (development only; use key management in production).</summary>
    public string? SigningKey { get; set; }

    /// <summary>Path to signing key material (preferred for production).</summary>
    public string? KeyPath { get; set; }
}

/// <summary>
/// Authentication cookie configuration options.
/// </summary>
public sealed class AuthCookieOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AuthCookies";

    /// <summary>Access token cookie name.</summary>
    public string AccessTokenName { get; set; } = "cb_access";

    /// <summary>Refresh token cookie name.</summary>
    public string RefreshTokenName { get; set; } = "cb_refresh";

    /// <summary>CSRF token cookie name.</summary>
    public string CsrfCookieName { get; set; } = "cb_csrf";

    /// <summary>CSRF header name.</summary>
    public string CsrfHeaderName { get; set; } = "X-CSRF-TOKEN";

    /// <summary>Cookie domain, if any.</summary>
    public string? Domain { get; set; }

    /// <summary>Require secure cookies.</summary>
    public bool Secure { get; set; } = true;
}
