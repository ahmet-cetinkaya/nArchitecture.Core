using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.ObjectPool;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;

namespace NArchitecture.Core.Application.Pipelines.Caching;

/// <summary>
/// Pipeline behavior that handles cache removal operations for requests implementing ICacheRemoverRequest.
/// Supports both individual cache key removal and group-based cache removal.
/// </summary>
public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheRemoverRequest
{
    private readonly IDistributedCache _cache;
    private readonly ILogger _logger;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private static readonly ObjectPool<HashSet<string>> HashSetPool = ObjectPool.Create(
        new DefaultPooledObjectPolicy<HashSet<string>>()
    );
    private static readonly int _bufferSize = 4096;

    public CacheRemovingBehavior(IDistributedCache cache, ILogger logger)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Check cancellation first
        cancellationToken.ThrowIfCancellationRequested();

        // Check if cache operations should be skipped
        if (request.CacheOptions.BypassCache)
            return await next();

        // Fast path for single key removal
        if (request.CacheOptions.CacheGroupKey == null && !string.IsNullOrEmpty(request.CacheOptions.CacheKey))
        {
            // Execute the next handler first
            TResponse response = await next();
            cancellationToken.ThrowIfCancellationRequested(); // Check before cache operation
            // Remove the single cache key
            await _cache.RemoveAsync(request.CacheOptions.CacheKey, cancellationToken);
            return response;
        }

        // Handle complex group key removal
        return await HandleWithGroupKeys(request, next, cancellationToken);
    }

    private async Task<TResponse> HandleWithGroupKeys(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Execute the next handler first
        TResponse response = await next();

        // Process group keys if present
        if (request.CacheOptions.CacheGroupKey?.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Rent memory buffer
            using IMemoryOwner<byte> bufferLease = MemoryPool<byte>.Shared.Rent(_bufferSize);
            // Remove group keys
            await RemoveGroupKeysAsync(request.CacheOptions.CacheGroupKey, bufferLease.Memory, cancellationToken);
        }

        // Remove single key if present
        if (!string.IsNullOrEmpty(request.CacheOptions.CacheKey))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _cache.RemoveAsync(request.CacheOptions.CacheKey, cancellationToken);
        }

        return response;
    }

    private async Task RemoveGroupKeysAsync(string[] groupKeys, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        // Initialize task list and key set from pool
        var pendingTasks = new List<Task>(capacity: 32);
        HashSet<string> keySet = HashSetPool.Get();

        try
        {
            // Process each group key
            foreach (string groupKey in groupKeys)
            {
                // Check for cancellation before each group processing
                cancellationToken.ThrowIfCancellationRequested();

                // Acquire group lock
                SemaphoreSlim @lock = Locks.GetOrAdd(groupKey, _ => new(1, 1));
                await @lock.WaitAsync(cancellationToken);

                try
                {
                    // Retrieve group data
                    byte[]? cachedGroup = await _cache.GetAsync(groupKey, cancellationToken);
                    if (cachedGroup == null)
                        continue;

                    // Prepare key set
                    keySet.Clear();
                    var reader = new Utf8JsonReader(cachedGroup);
                    if (JsonSerializer.Deserialize<string[]>(ref reader) is { } keys)
                        keySet.UnionWith(keys);

                    // Check cancellation before starting removal tasks
                    cancellationToken.ThrowIfCancellationRequested();

                    // Queue group and sliding expiration keys for removal
                    pendingTasks.Add(_cache.RemoveAsync(groupKey, cancellationToken));
                    pendingTasks.Add(_cache.RemoveAsync($"{groupKey}SlidingExpiration", cancellationToken));

                    // Queue each key in group for removal
                    foreach (string key in keySet)
                    {
                        pendingTasks.Add(_cache.RemoveAsync(key, cancellationToken));
                    }

                    // Execute all removal tasks
                    await Task.WhenAll(pendingTasks);
                    pendingTasks.Clear();

                    // Log the operation
                    await _logger.InformationAsync(
                        $"Removed cache group {groupKey} with {keySet.Count} keys"
                    );
                }
                finally
                {
                    _ = @lock.Release();
                }
            }
        }
        finally
        {
            HashSetPool.Return(keySet);
        }
    }
}
