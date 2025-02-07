namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Enables cache removal operations.
/// </summary>
public interface ICacheRemoverRequest
{
    /// <summary>
    /// Skip cache removal when true.
    /// </summary>
    bool BypassCache { get; }

    /// <summary>
    /// Single cache key to remove.
    /// </summary>
    string? CacheKey { get; }

    /// <summary>
    /// Group keys for bulk removal.
    /// </summary>
    string[]? CacheGroupKey { get; }
}
