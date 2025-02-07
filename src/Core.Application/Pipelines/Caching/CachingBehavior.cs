using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace NArchitecture.Core.Application.Pipelines.Caching;

public sealed class CachingBehavior<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger,
    IConfiguration configuration
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { DefaultBufferSize = 4096 };
    private static readonly ObjectPool<byte[]> s_byteArrayPool = new DefaultObjectPool<byte[]>(
        new ByteArrayPooledObjectPolicy(4096),
        50
    );
    private static readonly ObjectPool<DistributedCacheEntryOptions> s_cacheOptionsPool =
        new DefaultObjectPool<DistributedCacheEntryOptions>(new DefaultPooledObjectPolicy<DistributedCacheEntryOptions>(), 20);
    private readonly CacheSettings _cacheSettings = InitializeCacheSettings(configuration);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.BypassCache)
            return await next();

        byte[]? cachedResponse = await cache.GetAsync(request.CacheKey, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (cachedResponse is not null)
        {
            try
            {
                var response = DeserializeFromUtf8Bytes<TResponse>(cachedResponse);
                logger.LogInformation("Cache hit: {CacheKey}", request.CacheKey);
                return response;
            }
            catch
            {
                logger.LogWarning("Cache deserialization failed: {CacheKey}", request.CacheKey);
            }
        }

        return await GetResponseAndAddToCache(request, next, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T DeserializeFromUtf8Bytes<T>(ReadOnlySpan<byte> utf8Json)
    {
        var result = JsonSerializer.Deserialize<T>(utf8Json, s_jsonOptions);
        return result!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CacheSettings InitializeCacheSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("CacheSettings");
        var slidingExpirationStr = section["SlidingExpiration"];

        if (string.IsNullOrEmpty(slidingExpirationStr))
            throw new InvalidOperationException("Cache settings are not configured: SlidingExpiration is missing");

        if (!TimeSpan.TryParse(slidingExpirationStr, out TimeSpan slidingExpiration))
            throw new InvalidOperationException("Cache settings are invalid: SlidingExpiration must be a valid TimeSpan");

        if (slidingExpiration <= TimeSpan.Zero)
            throw new InvalidOperationException("Cache settings are invalid: SlidingExpiration must be positive");

        return new(slidingExpiration);
    }

    private async ValueTask<TResponse> GetResponseAndAddToCache(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        TResponse response = await next();

        TimeSpan slidingExpiration = request.SlidingExpiration ?? _cacheSettings.SlidingExpiration;
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be positive");

        var cacheOptions = s_cacheOptionsPool.Get();
        try
        {
            cacheOptions.SlidingExpiration = slidingExpiration;
            using var serializedData = SerializeToPooledUtf8Bytes(response);
            await cache.SetAsync(request.CacheKey, serializedData.Array, cacheOptions, cancellationToken);

            if (request.CacheGroupKey is not null)
                await AddCacheKeyToGroup(request, slidingExpiration, cancellationToken);

            return response;
        }
        finally
        {
            s_cacheOptionsPool.Return(cacheOptions);
        }
    }

    private sealed class PooledByteArray : IDisposable
    {
        public byte[] Array { get; }

        public PooledByteArray(byte[] array) => Array = array;

        public void Dispose() => s_byteArrayPool.Return(Array);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PooledByteArray SerializeToPooledUtf8Bytes<T>(T value)
    {
        byte[] rentedArray = s_byteArrayPool.Get();
        try
        {
            var tempArray = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), s_jsonOptions);
            if (tempArray.Length > rentedArray.Length)
            {
                s_byteArrayPool.Return(rentedArray);
                rentedArray = new byte[tempArray.Length];
            }
            tempArray.CopyTo(rentedArray, 0);
            return new PooledByteArray(rentedArray[..tempArray.Length]);
        }
        catch
        {
            s_byteArrayPool.Return(rentedArray);
            throw;
        }
    }

    private async Task AddCacheKeyToGroup(TRequest request, TimeSpan slidingExpiration, CancellationToken cancellationToken)
    {
        var groupKey = request.CacheGroupKey!;
        byte[]? groupCache = await cache.GetAsync(groupKey, cancellationToken);

        using var cacheKeysInGroup = new PooledSet<string>(
            capacity: 16,
            existing: groupCache is not null ? JsonSerializer.Deserialize<HashSet<string>>(groupCache, s_jsonOptions) : null
        );

        if (cacheKeysInGroup.Add(request.CacheKey))
        {
            var cacheOptions = s_cacheOptionsPool.Get();
            try
            {
                cacheOptions.SlidingExpiration = await GetOrUpdateGroupSlidingExpiration(
                    groupKey,
                    slidingExpiration,
                    cancellationToken
                );

                using var serializedGroup = SerializeToPooledUtf8Bytes(cacheKeysInGroup.Inner);
                await cache.SetAsync(groupKey, serializedGroup.Array, cacheOptions, cancellationToken);
            }
            finally
            {
                s_cacheOptionsPool.Return(cacheOptions);
            }
        }
    }

    private async ValueTask<TimeSpan> GetOrUpdateGroupSlidingExpiration(
        string groupKey,
        TimeSpan newExpiration,
        CancellationToken cancellationToken
    )
    {
        const int MaxStackSize = 128;
        byte[]? rentedArray = null;
        byte[]? resultBuffer = null;

        try
        {
            var expirationKey = $"{groupKey}SlidingExpiration";
            var existingExpirationBytes = await cache.GetAsync(expirationKey, cancellationToken);

            var finalExpiration = existingExpirationBytes is not null
                ? TimeSpan.FromSeconds(Math.Max(BitConverter.ToInt32(existingExpirationBytes), (int)newExpiration.TotalSeconds))
                : newExpiration;

            int totalSeconds = (int)finalExpiration.TotalSeconds;

            // Handle the buffer allocation
            if (totalSeconds <= MaxStackSize)
            {
                Span<byte> stackBuffer = stackalloc byte[MaxStackSize];
                if (Utf8Formatter.TryFormat(totalSeconds, stackBuffer, out int bytesWritten))
                {
                    resultBuffer = stackBuffer[..bytesWritten].ToArray();
                }
            }
            else
            {
                rentedArray = ArrayPool<byte>.Shared.Rent(totalSeconds);
                Span<byte> rentedSpan = rentedArray;
                if (Utf8Formatter.TryFormat(totalSeconds, rentedSpan, out int bytesWritten))
                {
                    resultBuffer = rentedSpan[..bytesWritten].ToArray();
                }
            }

            if (resultBuffer != null)
            {
                await cache.SetAsync(
                    expirationKey,
                    resultBuffer,
                    new DistributedCacheEntryOptions { SlidingExpiration = finalExpiration },
                    cancellationToken
                );
            }

            return finalExpiration;
        }
        finally
        {
            if (rentedArray is not null)
                ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }
}

internal sealed class PooledSet<T> : IDisposable
{
    private static readonly ObjectPool<HashSet<T>> s_setPool = new DefaultObjectPool<HashSet<T>>(
        new SetPooledObjectPolicy<T>(),
        20
    );

    public HashSet<T> Inner { get; }

    public PooledSet(int capacity = 4, HashSet<T>? existing = null)
    {
        Inner = s_setPool.Get();
        Inner.Clear();
        if (existing != null)
            Inner.UnionWith(existing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T item) => Inner.Add(item);

    public void Dispose() => s_setPool.Return(Inner);
}

internal sealed class SetPooledObjectPolicy<T> : IPooledObjectPolicy<HashSet<T>>
{
    public HashSet<T> Create() => new(capacity: 4);

    public bool Return(HashSet<T> obj) => true;
}

internal sealed class ByteArrayPooledObjectPolicy : IPooledObjectPolicy<byte[]>
{
    private readonly int _defaultSize;

    public ByteArrayPooledObjectPolicy(int defaultSize) => _defaultSize = defaultSize;

    public byte[] Create() => new byte[_defaultSize];

    public bool Return(byte[] obj) => obj.Length == _defaultSize;
}
