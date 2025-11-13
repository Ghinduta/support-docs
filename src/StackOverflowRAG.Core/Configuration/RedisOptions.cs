using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for Redis caching
/// </summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string (e.g., localhost:6379)
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Default TTL (Time To Live) for cached items in hours
    /// </summary>
    [Range(1, 168)] // 1 hour to 7 days
    public int CacheTtlHours { get; set; } = 24;

    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Redis ConnectionString cannot be null or empty");
        }

        if (CacheTtlHours < 1 || CacheTtlHours > 168)
        {
            throw new InvalidOperationException("CacheTtlHours must be between 1 and 168 hours (7 days)");
        }
    }

    /// <summary>
    /// Gets the TTL as a TimeSpan
    /// </summary>
    public TimeSpan GetTtl() => TimeSpan.FromHours(CacheTtlHours);
}
