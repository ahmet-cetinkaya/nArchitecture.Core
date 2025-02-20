namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Groups caching options into a struct to avoid unnecessary allocations.
/// </summary>
public readonly struct CacheableOptions
{
    /// <summary>
    /// Gets a value indicating whether caching should be bypassed.
    /// </summary>
    public bool BypassCache { get; init; }

    /// <summary>
    /// Gets the unique cache key.
    /// </summary>
    public string CacheKey { get; init; }

    /// <summary>
    /// Gets the optional group key for bulk cache operations.
    /// </summary>
    public string? CacheGroupKey { get; init; }

    /// <summary>
    /// Gets the optional sliding expiration time.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    public static CacheableOptions Default =>
        new()
        {
            BypassCache = false,
            CacheKey = string.Empty,
            CacheGroupKey = null,
            SlidingExpiration = null,
        };

    public CacheableOptions(bool bypassCache, string cacheKey, string? cacheGroupKey, TimeSpan? slidingExpiration)
    {
        BypassCache = bypassCache;
        CacheKey = cacheKey;
        CacheGroupKey = cacheGroupKey;
        SlidingExpiration = slidingExpiration;
    }
}
