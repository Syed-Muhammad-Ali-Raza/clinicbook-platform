using System.Net;
using Microsoft.Extensions.Primitives;

namespace ClinicBook.Api.Middleware;

/// <summary>
/// Enforces basic CSRF protections (Origin check and double-submit token) for state-changing requests.
/// </summary>
public sealed class CsrfProtectionMiddleware
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="CsrfProtectionMiddleware"/> class.</summary>
    public CsrfProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!UnsafeMethods.Contains(context.Request.Method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!HasValidOrigin(context))
        {
            await WriteForbiddenAsync(context, "Invalid request origin.").ConfigureAwait(false);
            return;
        }

        // Double-submit: require CSRF cookie and matching header for all state-changing requests.
        // Clients must obtain the CSRF cookie via a safe request (GET /api/v1/auth/csrf) before unsafe requests.
        var csrfCookieName = _configuration["AuthCookies:CsrfCookieName"] ?? "cb_csrf";
        var csrfHeaderName = _configuration["AuthCookies:CsrfHeaderName"] ?? "X-CSRF-TOKEN";
        context.Request.Cookies.TryGetValue(csrfCookieName, out var cookieToken);
        StringValues headerValues = default;
        context.Request.Headers.TryGetValue(csrfHeaderName, out headerValues);
        var headerToken = headerValues.ToString();

        if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken) || !FixedTimeEquals(cookieToken, headerToken))
        {
            await WriteForbiddenAsync(context, "CSRF token validation failed.").ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private bool HasValidOrigin(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        var referer = context.Request.Headers.Referer.ToString();
        var candidate = !string.IsNullOrWhiteSpace(origin) ? origin : referer;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            // Non-browser or same-origin requests may omit Origin/Referer; allow when host matches (best-effort).
            return true;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Any(o => string.Equals(o.TrimEnd('/'), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Allow same-host requests.
        var requestHost = $"{context.Request.Scheme}://{context.Request.Host}";
        return string.Equals(requestHost, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/problem+json";
        var problem = new
        {
            status = 403,
            title = "Forbidden",
            detail,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier
        };
        await context.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
    }
}
