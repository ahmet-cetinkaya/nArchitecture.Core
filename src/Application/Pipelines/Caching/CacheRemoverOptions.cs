namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Groups cache removal options into a struct to reduce allocation and command clutter.
/// </summary>
public readonly struct CacheRemoverOptions
{
    /// <summary>
    /// Gets a value indicating whether cache removal should be bypassed.
    /// </summary>
    public bool BypassCache { get; init; }

    /// <summary>
    /// Gets the cache key to remove.
    /// </summary>
    public string? CacheKey { get; init; }

    /// <summary>
    /// Gets the group keys for bulk cache removal.
    /// </summary>
    public string[]? CacheGroupKey { get; init; }

    public static CacheRemoverOptions Default =>
        new()
        {
            BypassCache = false,
            CacheKey = string.Empty,
            CacheGroupKey = [],
        };

    public CacheRemoverOptions(bool bypassCache, string? cacheKey, string[]? cacheGroupKey)
    {
        BypassCache = bypassCache;
        CacheKey = cacheKey;
        CacheGroupKey = cacheGroupKey;
    }
}
