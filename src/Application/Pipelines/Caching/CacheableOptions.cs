namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Groups caching options into a record to avoid unnecessary copying overhead.
/// </summary>
public record CacheableOptions(bool BypassCache, string CacheKey, string? CacheGroupKey, TimeSpan? SlidingExpiration)
{
    public static CacheableOptions Default { get; } =
        new(BypassCache: false, CacheKey: string.Empty, CacheGroupKey: null, SlidingExpiration: null);
}
