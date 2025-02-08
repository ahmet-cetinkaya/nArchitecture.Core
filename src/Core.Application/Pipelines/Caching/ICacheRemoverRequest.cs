namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Enables cache removal operations.
/// </summary>
public interface ICacheRemoverRequest
{
    CacheRemoverOptions CacheOptions => CacheRemoverOptions.Default;
}
