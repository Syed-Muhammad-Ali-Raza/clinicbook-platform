namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Reads and writes authentication cookies and CSRF tokens for the current HTTP request.
/// </summary>
public interface IAuthCookieManager
{
    /// <summary>Gets the refresh token from cookies, if present.</summary>
    string? GetRefreshToken();

    /// <summary>Gets the CSRF token from the request header, if present.</summary>
    string? GetCsrfHeaderToken();

    /// <summary>Gets the CSRF token from cookies, if present.</summary>
    string? GetCsrfCookieToken();

    /// <summary>Sets authentication cookies for a successful login/refresh.</summary>
    /// <param name="accessToken">JWT access token.</param>
    /// <param name="accessTokenExpiresAtUtc">Access token expiry.</param>
    /// <param name="refreshToken">Opaque refresh token.</param>
    /// <param name="refreshTokenExpiresAtUtc">Refresh token expiry.</param>
    /// <param name="csrfToken">CSRF token for double-submit protection.</param>
    void SetAuthCookies(string accessToken, DateTime accessTokenExpiresAtUtc, string refreshToken, DateTime refreshTokenExpiresAtUtc, string csrfToken);

    /// <summary>Clears authentication cookies.</summary>
    void ClearAuthCookies();
}
