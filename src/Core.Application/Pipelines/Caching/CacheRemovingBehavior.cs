using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Pipeline behavior that handles cache removal operations for requests implementing ICacheRemoverRequest.
/// Supports both individual cache key removal and group-based cache removal.
/// </summary>
public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheRemovingBehavior<TRequest, TResponse>> _logger;

    public CacheRemovingBehavior(IDistributedCache cache, ILogger<CacheRemovingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Handles the cache removal operations for the request.
    /// </summary>
    /// <param name="request">The request containing cache keys to remove</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the next handler</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Skip cache operations if bypassing cache
        if (request.BypassCache)
            return await next();

        // Execute the request handler first
        TResponse response = await next();

        // Process group-based cache removal
        if (request.CacheGroupKey != null)
            for (int i = 0; i < request.CacheGroupKey.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string groupKey = request.CacheGroupKey[i];

                // Get the cached group data
                byte[]? cachedGroup = await _cache.GetAsync(groupKey, cancellationToken);
                if (cachedGroup != null)
                {
                    // Deserialize and remove all keys in the group
                    HashSet<string> keysInGroup = JsonSerializer.Deserialize<HashSet<string>>(
                        Encoding.Default.GetString(cachedGroup)
                    )!;
                    foreach (string key in keysInGroup)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Remove individual cache entry
                        await _cache.RemoveAsync(key, cancellationToken);
                        _logger.LogInformation($"Removed Cache -> {key}");
                    }

                    // Remove group metadata
                    await _cache.RemoveAsync(groupKey, cancellationToken);
                    _logger.LogInformation($"Removed Cache -> {groupKey}");

                    // Remove sliding expiration data
                    await _cache.RemoveAsync($"{groupKey}SlidingExpiration", cancellationToken);
                    _logger.LogInformation($"Removed Cache -> {groupKey}SlidingExpiration");
                }
            }

        // Process individual cache key removal
        if (request.CacheKey != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _cache.RemoveAsync(request.CacheKey, cancellationToken);
            _logger.LogInformation($"Removed Cache -> {request.CacheKey}");
        }

        return response;
    }
}
