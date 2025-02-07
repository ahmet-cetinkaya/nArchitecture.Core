namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Cache configuration options.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Default sliding expiration duration.
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; }
}
