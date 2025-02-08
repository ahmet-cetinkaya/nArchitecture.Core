﻿using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Implements caching behavior for MediatR pipeline using IDistributedCache.
/// Handles cache operations for requests implementing ICacheableRequest.
/// </summary>
/// <typeparam name="TRequest">The type of request implementing ICacheableRequest</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public sealed class CachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger,
    IConfiguration configuration
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly CacheSettings _cacheSettings = InitializeCacheSettings(configuration);

    /// <summary>
    /// Handles the request with caching support. Returns cached data if available, otherwise executes the request and caches the response.
    /// </summary>
    /// <param name="request">The request containing caching parameters</param>
    /// <param name="next">The delegate to execute the request if cache miss</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from cache or from executing the request</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Early exit if caching is bypassed
        if (request.BypassCache)
            return await next();

        // Try to get from cache
        byte[]? cachedResponse = await cache.GetAsync(request.CacheKey, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (cachedResponse is not null)
        {
            try
            {
                // Deserialize and return cached response
                TResponse? response = DeserializeFromUtf8Bytes<TResponse>(cachedResponse);
                logger.LogInformation("Cache hit: {CacheKey}", request.CacheKey);
                return response;
            }
            catch
            {
                // Log deserialization failures but continue with cache miss path
                logger.LogWarning("Cache deserialization failed: {CacheKey}", request.CacheKey);
            }
        }

        // Cache miss - get fresh response
        return await getResponseAndAddToCache(request, next, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T DeserializeFromUtf8Bytes<T>(ReadOnlySpan<byte> utf8Json)
    {
        T? result = JsonSerializer.Deserialize<T>(utf8Json, JsonOptions);
        return result!;
    }

    /// <summary>
    /// Initializes cache settings from configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when cache settings are missing or invalid</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheSettings InitializeCacheSettings(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("CacheSettings");
        string? slidingExpirationStr = section["SlidingExpiration"];

        if (string.IsNullOrEmpty(slidingExpirationStr))
            throw new InvalidOperationException("Cache settings are not configured: SlidingExpiration is missing");

        if (!TimeSpan.TryParse(slidingExpirationStr, out TimeSpan slidingExpiration))
            throw new InvalidOperationException("Cache settings are invalid: SlidingExpiration must be a valid TimeSpan");

        if (slidingExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("Cache settings are invalid: SlidingExpiration must be positive");

        return new(slidingExpiration);
    }

    /// <summary>
    /// Gets response from next handler and adds it to cache.
    /// </summary>
    private async ValueTask<TResponse> getResponseAndAddToCache(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Get fresh response from handler
        TResponse response = await next();

        // Validate and get sliding expiration
        TimeSpan slidingExpiration = request.SlidingExpiration ?? _cacheSettings.SlidingExpiration;
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be positive");

        // Cache the response
        var cacheOptions = new DistributedCacheEntryOptions { SlidingExpiration = slidingExpiration };
        byte[] serializedData = JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
        await cache.SetAsync(request.CacheKey, serializedData, cacheOptions, cancellationToken);

        // Add to cache group if specified
        if (request.CacheGroupKey is not null)
            await addCacheKeyToGroup(request, slidingExpiration, cancellationToken);

        return response;
    }

    /// <summary>
    /// Adds the cache key to a cache group for batch invalidation support.
    /// </summary>
    private async Task addCacheKeyToGroup(TRequest request, TimeSpan slidingExpiration, CancellationToken cancellationToken)
    {
        // Get existing group cache
        string groupKey = request.CacheGroupKey!;
        byte[]? groupCache = await cache.GetAsync(groupKey, cancellationToken);

        // Get or create cache keys set
        HashSet<string> cacheKeysInGroup = groupCache is not null
            ? JsonSerializer.Deserialize<HashSet<string>>(groupCache, JsonOptions) ?? []
            : [];

        // Add new key to group if not exists
        if (cacheKeysInGroup.Add(request.CacheKey))
        {
            // Update group cache with new expiration
            DistributedCacheEntryOptions cacheOptions = new()
            {
                SlidingExpiration = await getOrUpdateGroupSlidingExpiration(groupKey, slidingExpiration, cancellationToken),
            };

            byte[] serializedGroup = JsonSerializer.SerializeToUtf8Bytes(cacheKeysInGroup, JsonOptions);
            await cache.SetAsync(groupKey, serializedGroup, cacheOptions, cancellationToken);
        }
    }

    /// <summary>
    /// Updates or sets the sliding expiration for a cache group.
    /// </summary>
    private async ValueTask<TimeSpan> getOrUpdateGroupSlidingExpiration(
        string groupKey,
        TimeSpan newExpiration,
        CancellationToken cancellationToken
    )
    {
        // Get existing expiration
        string expirationKey = $"{groupKey}SlidingExpiration";
        byte[]? existingExpirationBytes = await cache.GetAsync(expirationKey, cancellationToken);

        // Calculate final expiration (max of existing and new)
        int newSeconds = (int)newExpiration.TotalSeconds;
        int finalSeconds;

        if (existingExpirationBytes != null)
        {
            int existingSeconds = BitConverter.ToInt32(existingExpirationBytes);
            finalSeconds = Math.Max(existingSeconds, newSeconds);
        }
        else
        {
            finalSeconds = newSeconds;
        }

        // Update expiration in cache
        var finalExpiration = TimeSpan.FromSeconds(finalSeconds);
        byte[] secondsBytes = BitConverter.GetBytes(finalSeconds);

        await cache.SetAsync(
            expirationKey,
            secondsBytes,
            new DistributedCacheEntryOptions { SlidingExpiration = finalExpiration },
            cancellationToken
        );

        return finalExpiration;
    }
}
