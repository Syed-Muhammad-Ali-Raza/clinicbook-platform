using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Manages authentication cookies for the current HTTP response.
/// </summary>
public sealed class AuthCookieManager : ClinicBook.Application.Common.Interfaces.IAuthCookieManager
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthCookieOptions _options;
    private readonly IHostEnvironmentAdapter _hostEnvironment;

    /// <summary>Initializes a new instance of the <see cref="AuthCookieManager"/> class.</summary>
    public AuthCookieManager(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthCookieOptions> options,
        IHostEnvironmentAdapter hostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("No active HTTP context.");

    /// <inheritdoc />
    public string? GetRefreshToken()
    {
        return HttpContext.Request.Cookies.TryGetValue(_options.RefreshTokenName, out var value) ? value : null;
    }

    /// <inheritdoc />
    public string? GetCsrfHeaderToken()
    {
        return HttpContext.Request.Headers.TryGetValue(_options.CsrfHeaderName, out var values) ? values.ToString() : null;
    }

    /// <inheritdoc />
    public string? GetCsrfCookieToken()
    {
        return HttpContext.Request.Cookies.TryGetValue(_options.CsrfCookieName, out var value) ? value : null;
    }

    /// <inheritdoc />
    public void SetAuthCookies(string accessToken, DateTime accessTokenExpiresAtUtc, string refreshToken, DateTime refreshTokenExpiresAtUtc, string csrfToken)
    {
        var secure = _options.Secure;

        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(accessTokenExpiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true,
            Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain
        };

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(refreshTokenExpiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true,
            Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain
        };

        var csrfOptions = new CookieOptions
        {
            HttpOnly = false, // must be readable by client for double-submit pattern
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(refreshTokenExpiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true,
            Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain
        };

        var response = HttpContext.Response;
        response.Cookies.Append(_options.AccessTokenName, accessToken, accessOptions);
        response.Cookies.Append(_options.RefreshTokenName, refreshToken, refreshOptions);
        response.Cookies.Append(_options.CsrfCookieName, csrfToken, csrfOptions);
    }

    /// <inheritdoc />
    public void ClearAuthCookies()
    {
        var secure = _options.Secure;
        var response = HttpContext.Response;

        void Delete(string name, string path, bool httpOnly)
        {
            response.Cookies.Append(name, string.Empty, new CookieOptions
            {
                HttpOnly = httpOnly,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = path,
                Expires = DateTimeOffset.UnixEpoch,
                IsEssential = true,
                Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain
            });
        }

        Delete(_options.AccessTokenName, "/api", true);
        Delete(_options.RefreshTokenName, "/api/v1/auth", true);
        Delete(_options.CsrfCookieName, "/", false);
    }
}

/// <summary>
/// Minimal environment adapter for infrastructure.
/// </summary>
public interface IHostEnvironmentAdapter
{
    /// <summary>Gets whether running in Development.</summary>
    bool IsDevelopment();

    /// <summary>Gets whether insecure cookies are explicitly allowed in development.</summary>
    bool IsDevelopmentInsecureCookiesAllowed();
}

/// <summary>
/// Default host environment adapter.
/// </summary>
public sealed class HostEnvironmentAdapter : IHostEnvironmentAdapter
{
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;
    private readonly AuthCookieOptions _cookieOptions;

    /// <summary>Initializes adapter.</summary>
    public HostEnvironmentAdapter(Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment, IOptions<AuthCookieOptions> cookieOptions)
    {
        _environment = environment;
        _cookieOptions = cookieOptions.Value;
    }

    /// <inheritdoc />
    public bool IsDevelopment() => string.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsDevelopmentInsecureCookiesAllowed() => IsDevelopment() && !_cookieOptions.Secure;
}
