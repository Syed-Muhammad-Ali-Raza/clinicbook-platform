using ClinicBook.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Application.Common.Behaviors;

/// <summary>
/// Caches responses for queries implementing <see cref="ICacheableQuery"/>.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(1);

    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behavior.</summary>
    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next().ConfigureAwait(false);
        }

        var cacheKey = cacheableQuery.CacheKey;
        var cached = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        var response = await next().ConfigureAwait(false);
        var duration = cacheableQuery.CacheDuration ?? DefaultDuration;
        await _cacheService.SetAsync(cacheKey, response, duration, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Cached response for {CacheKey} ({Duration})", cacheKey, duration);
        return response;
    }
}
