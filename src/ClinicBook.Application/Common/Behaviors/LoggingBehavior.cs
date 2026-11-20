#pragma warning disable CA1848
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Application.Common.Behaviors;

/// <summary>
/// Logs request execution and duration.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behavior.</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unhandled error handling {RequestName} after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
#pragma warning restore CA1848
