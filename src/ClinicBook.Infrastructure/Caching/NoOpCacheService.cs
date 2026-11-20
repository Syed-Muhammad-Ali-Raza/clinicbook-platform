using ClinicBook.Application.Common.Interfaces;

namespace ClinicBook.Infrastructure.Caching;

/// <summary>
/// No-op cache implementation used when distributed caching is unavailable.
/// </summary>
public sealed class NoOpCacheService : ICacheService
{
    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
