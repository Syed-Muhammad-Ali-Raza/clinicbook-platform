namespace ClinicBook.Api.Middleware;

/// <summary>
/// Adds standard security headers and removes identifying headers.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                // Strict baseline; Swagger in development may need relaxed CSP, but Swagger is dev-only.
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            }
            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }
}
