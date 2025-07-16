namespace NArchitecture.Core.Application.Pipelines.Caching;

public record struct CacheSettings
{
    public TimeSpan SlidingExpiration { get; }

    public CacheSettings(TimeSpan slidingExpiration)
    {
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be positive");
        SlidingExpiration = slidingExpiration;
    }
}
