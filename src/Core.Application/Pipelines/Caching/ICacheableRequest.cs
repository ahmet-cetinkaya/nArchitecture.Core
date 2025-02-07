namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Enables caching for request responses.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Skip caching when true.
    /// </summary>
    bool BypassCache { get; }

    /// <summary>
    /// Unique key for the cache entry.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional group key for bulk cache operations.
    /// </summary>
    string? CacheGroupKey { get; }

    /// <summary>
    /// Optional cache duration, defaults to CacheSettings value.
    /// </summary>
    TimeSpan? SlidingExpiration { get; }
}
