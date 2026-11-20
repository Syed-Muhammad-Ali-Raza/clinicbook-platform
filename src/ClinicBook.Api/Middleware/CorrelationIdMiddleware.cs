namespace ClinicBook.Api.Middleware;

/// <summary>
/// Ensures each request has a correlation/trace identifier.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>HTTP header name for correlation id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    /// <summary>Initializes the middleware.</summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values) && !string.IsNullOrWhiteSpace(values)
            ? values.ToString()
            : context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = correlationId;
            }

            return Task.CompletedTask;
        });

        using (_ = Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
