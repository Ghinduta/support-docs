namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for Redis caching with TTL (Time To Live)
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value as string, or null if not found</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value with TTL
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="ttl">Time to live</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
