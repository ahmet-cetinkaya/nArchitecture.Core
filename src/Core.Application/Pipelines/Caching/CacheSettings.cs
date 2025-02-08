namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Cache configuration options.
/// </summary>
public readonly record struct CacheSettings
{
    /// <summary>
    /// Default sliding expiration duration.
    /// </summary>
    public TimeSpan SlidingExpiration { get; init; }

    public CacheSettings(TimeSpan slidingExpiration)
    {
        SlidingExpiration = slidingExpiration;
    }
}
