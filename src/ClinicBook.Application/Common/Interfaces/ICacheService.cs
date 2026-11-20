namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Abstraction for distributed caching.
/// </summary>
public interface ICacheService
{
    /// <summary>Gets a cached value by key.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Sets a cached value.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a cached value.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
