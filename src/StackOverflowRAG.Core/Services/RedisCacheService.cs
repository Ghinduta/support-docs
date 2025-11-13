using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Redis-based cache service implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("RedisCacheService initialized with TTL: {Ttl}h", _options.CacheTtlHours);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache disabled, skipping GET for key: {Key}", key);
            return null;
        }

        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.HasValue)
            {
                _logger.LogDebug("Cache HIT for key: {Key}", key);
                return value.ToString();
            }

            _logger.LogDebug("Cache MISS for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return null; // Fail gracefully, return null on Redis errors
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Cache disabled, skipping SET for key: {Key}", key);
            return;
        }

        try
        {
            await _database.StringSetAsync(key, value, ttl);
            _logger.LogDebug("Cache SET for key: {Key}, TTL: {Ttl}s", key, ttl.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            // Fail gracefully, don't throw on Redis errors
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return false; // Fail gracefully
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Cache DELETE for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache key: {Key}", key);
            // Fail gracefully
        }
    }
}
