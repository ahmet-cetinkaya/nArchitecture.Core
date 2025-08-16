namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Groups cache removal options into a record to reduce copying overhead.
/// </summary>
public record CacheRemoverOptions(bool BypassCache, string? CacheKey, string[]? CacheGroupKey)
{
    public static CacheRemoverOptions Default { get; } = new(BypassCache: false, CacheKey: string.Empty, CacheGroupKey: []);
}
