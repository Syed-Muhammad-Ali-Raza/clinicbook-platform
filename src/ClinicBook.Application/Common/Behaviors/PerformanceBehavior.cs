#pragma warning disable CA1848
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Application.Common.Behaviors;

/// <summary>
/// Warns when request handling exceeds a performance threshold.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMilliseconds = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behavior.</summary>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop();

        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed > WarningThresholdMilliseconds)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Long running request: {RequestName} took {ElapsedMilliseconds}ms (threshold {ThresholdMilliseconds}ms)",
                requestName,
                elapsed,
                WarningThresholdMilliseconds);
        }

        return response;
    }
}
#pragma warning restore CA1848
