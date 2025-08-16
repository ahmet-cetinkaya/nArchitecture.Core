namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Enables caching for request responses.
/// </summary>
public interface ICacheableRequest
{
    CacheableOptions CacheOptions => CacheableOptions.Default;
}
