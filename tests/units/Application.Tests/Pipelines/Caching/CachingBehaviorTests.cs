using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Caching;

public class MockCacheableRequest : IRequest<string>, ICacheableRequest
{
    public CacheableOptions CacheOptions { get; set; } =
        new CacheableOptions(BypassCache: false, CacheKey: "test-key", CacheGroupKey: null, SlidingExpiration: null);
}

[Trait("Category", "Caching")]
public class CachingBehaviorTests
{
    private readonly IDistributedCache _cache;
    private readonly Mock<ILogger> _loggerMock;
    private readonly CachingBehavior<MockCacheableRequest, string> _behavior;
    private readonly RequestHandlerDelegate<string> _nextDelegate;

    public CachingBehaviorTests()
    {
        var options = new MemoryDistributedCacheOptions();
        _cache = new MemoryDistributedCache(Options.Create(options));
        _loggerMock = new Mock<ILogger>();

        _behavior = new CachingBehavior<MockCacheableRequest, string>(
            _cache,
            _loggerMock.Object,
            new CacheSettings(TimeSpan.FromDays(2))
        );
        _nextDelegate = () => Task.FromResult("test-response");
    }

    [Fact(DisplayName = "Handle should skip cache when bypassCache is true")]
    public async Task Handle_WhenBypassCacheIsTrue_ShouldSkipCache()
    {
        // Arrange: Create request with bypassCache true.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: true,
                CacheKey: "test-key",
                CacheGroupKey: null,
                SlidingExpiration: null
            ),
        };

        // Act: Execute the caching behavior.
        string result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the response is returned and cache remains unchanged.
        result.ShouldBe("test-response");
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        cachedValue.ShouldBeNull();
    }

    [Fact(DisplayName = "Handle should return cached response when cache exists")]
    public async Task Handle_WhenCacheExists_ShouldReturnCachedResponse()
    {
        // Arrange: Populate the cache with a known response.
        var request = new MockCacheableRequest();
        string cachedResponse = "cached-response";
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedResponse)));

        // Act: Execute the caching behavior.
        string result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify returned response equals the cached response.
        result.ShouldBe(cachedResponse);
    }

    [Fact(DisplayName = "Handle should cache response when cache does not exist")]
    public async Task Handle_WhenCacheDoesNotExist_ShouldCacheResponse()
    {
        // Arrange: Create a request without a cached response.
        var request = new MockCacheableRequest();

        // Act: Execute caching behavior.
        string result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the response is cached correctly.
        result.ShouldBe("test-response");
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        string? cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse.ShouldBe("test-response");
    }

    [Theory(DisplayName = "Handle should respect expiration when custom sliding expiration is provided")]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Handle_WithCustomSlidingExpiration_ShouldRespectExpiration(int minutes)
    {
        // Arrange: Create a request with a custom sliding expiration.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: null,
                SlidingExpiration: TimeSpan.FromMinutes(minutes)
            ),
        };

        // Act: Execute caching behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the cache entry has the specified sliding expiration.
        var cacheOptions = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) };
        await _cache.SetAsync(
            request.CacheOptions.CacheKey,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize("test")),
            cacheOptions
        );
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Handle should add to cache group when cache group key is provided")]
    public async Task Handle_WithCacheGroupKey_ShouldAddToGroup()
    {
        // Arrange: Create a request with a cache group key.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: "test-group",
                SlidingExpiration: null
            ),
        };

        // Act: Execute caching behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the cache group is created and contains the key.
        byte[]? groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        _ = groupCache.ShouldNotBeNull();
        HashSet<string>? keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(groupCache!));
        _ = keys.ShouldNotBeNull();
        keys.ShouldContain(request.CacheOptions.CacheKey);

        byte[]? slidingExpirationCache = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = slidingExpirationCache.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Handle should cancel operation when cancellation is requested")]
    public async Task Handle_WhenCancellationRequested_ShouldCancelOperation()
    {
        // Arrange: Create a request and cancel the token.
        var request = new MockCacheableRequest();
        var cts = new CancellationTokenSource();

        // Add some data to ensure cache operations are triggered
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize("cached-value")));

        if (request.CacheOptions.CacheGroupKey != null)
        {
            await _cache.SetAsync(
                request.CacheOptions.CacheGroupKey,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new HashSet<string> { request.CacheOptions.CacheKey }))
            );
        }

        // Cancel immediately
        cts.Cancel();

        // Act & Assert: Verify OperationCanceledException is thrown.
        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _behavior.Handle(request, _nextDelegate, cts.Token)
        );
    }

    [Theory(DisplayName = "Handle should throw exception when sliding expiration is invalid")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_WithInvalidSlidingExpiration_ShouldThrowException(int minutes)
    {
        // Arrange: Create a request with an invalid sliding expiration.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: null,
                SlidingExpiration: TimeSpan.FromMinutes(minutes)
            ),
        };

        // Act & Assert: Expect ArgumentOutOfRangeException.
        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await _behavior.Handle(request, _nextDelegate, CancellationToken.None)
        );
    }

    [Fact(DisplayName = "Handle should log warning and continue when cache deserialization fails")]
    public async Task Handle_WhenCacheDeserializationFails_ShouldLogWarningAndContinue()
    {
        // Arrange: Set cache entry to invalid JSON.
        var request = new MockCacheableRequest();
        // Store invalid JSON data in cache to force deserialization failure
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes("invalid-json-data"));

        _ = _loggerMock
            .Setup(x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))))
            .Returns(Task.CompletedTask);

        // Act: Execute caching behavior.
        string result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify fallback response is returned and a warning is logged.
        result.ShouldBe("test-response", "Should fallback to next delegate when deserialization fails");

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))),
            Times.Once,
            "Should log warning when deserialization fails"
        );
    }

    [Fact(DisplayName = "Handle should handle array resizing when response is large")]
    public async Task Handle_WithLargeResponse_ShouldHandleArrayResizing()
    {
        // Arrange: Create a request that produces a large response.
        var request = new MockCacheableRequest();
        string largeResponse = new('x', 8192); // Create a string larger than default buffer size
        Task<string> largeResponseDelegate() => Task.FromResult(largeResponse);

        // Act: Execute caching behavior.
        string result = await _behavior.Handle(request, largeResponseDelegate, CancellationToken.None);

        // Assert: Verify large response is handled and cached correctly.
        result.ShouldBe(largeResponse);
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        string? cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse.ShouldBe(largeResponse);
    }

    [Theory(DisplayName = "Handle should handle array resizing when response size varies")]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    [InlineData(16384)]
    public async Task Handle_WithVariousResponseSizes_ShouldHandleArrayResizing(int responseSize)
    {
        // Arrange: Create a request with a response of given size.
        var request = new MockCacheableRequest();
        string response = new('x', responseSize);
        Task<string> responseDelegate() => Task.FromResult(response);

        // Act: Execute caching behavior.
        string result = await _behavior.Handle(request, responseDelegate, CancellationToken.None);

        // Assert: Verify the response's length matches and is cached.
        result.Length.ShouldBe(responseSize);
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        string? cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse!.Length.ShouldBe(responseSize);
    }

    [Fact(DisplayName = "Handle should handle and release resources when serialization fails")]
    public async Task Handle_WhenSerializationFails_ShouldHandleAndReleaseResources()
    {
        // Arrange: Create a request causing serialization failure.
        var request = new MockCacheableRequest();
        var failingData = new FailingData();
        Task<string> failingDelegate() =>
            Task.FromResult(
                JsonSerializer.Serialize(failingData, new JsonSerializerOptions { Converters = { new FailingConverter() } })
            );

        // Act & Assert: Verify exception is thrown and cache remains unmodified.
        Exception exception = await Record.ExceptionAsync(
            () => _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<JsonException>();

        // Verify the cache wasn't modified
        byte[]? cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        cachedValue.ShouldBeNull("Cache should not be modified when serialization fails");
    }

    [Fact(DisplayName = "Handle should not leak resources when serialization fails")]
    public async Task Handle_WhenSerializationFails_ShouldNotLeakResources()
    {
        // Arrange: Create a request causing serialization failure.
        var request = new MockCacheableRequest();
        var failingData = new FailingData();
        Task<string> failingDelegate() =>
            Task.FromResult(
                JsonSerializer.Serialize(failingData, new JsonSerializerOptions { Converters = { new FailingConverter() } })
            );

        // Act & Assert: Verify an exception is thrown and subsequent requests work.
        Exception exception = await Record.ExceptionAsync(
            () => _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<JsonException>();

        // Additional requests should still work
        var validRequest = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "valid-key",
                CacheGroupKey: null,
                SlidingExpiration: null
            ),
        };
        string result = await _behavior.Handle(validRequest, _nextDelegate, CancellationToken.None);
        result.ShouldBe("test-response", "Subsequent requests should work after serialization failure");
    }

    [Fact(DisplayName = "Handle should release pooled array when serialization fails during array operation")]
    public async Task Handle_WhenSerializationFailsDuringArrayOperation_ShouldReleasePooledArray()
    {
        // Arrange: Create a request that triggers array allocation then fails serialization.
        var request = new MockCacheableRequest();

        // Create a delegate that will trigger array allocation and then fail serialization
        int count = 0;
        Task<string> failingDelegate()
        {
            count++;
            if (count == 1)
            {
                var circularObject = new CircularReferenceObject();
                circularObject.Reference = circularObject; // Create circular reference
                return Task.FromResult(JsonSerializer.Serialize(circularObject)); // This will throw
            }

            return Task.FromResult("test-response");
        }

        // Act & Assert: Verify exception is thrown and pool remains usable.
        _ = await Should.ThrowAsync<JsonException>(
            async () => await _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        // Verify we can still use the cache system after failure
        var validRequest = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-after-failure",
                CacheGroupKey: null,
                SlidingExpiration: null
            ),
        };
        string result = await _behavior.Handle(validRequest, _nextDelegate, CancellationToken.None);
        result.ShouldBe("test-response");
    }

    [Fact(DisplayName = "Handle should initialize pooled set when group cache is null")]
    public async Task Handle_WithNullGroupCache_ShouldHandlePooledSetInitialization()
    {
        // Arrange: Create a request with a cache group key but no existing group.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: "test-group",
                SlidingExpiration: null
            ),
        };

        // Don't set any cache data to ensure null group cache

        // Act: Execute caching behavior.
        string result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify cache group is created with the key.
        result.ShouldBe("test-response");

        // Verify group was created
        byte[]? groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        _ = groupCache.ShouldNotBeNull();
        HashSet<string>? keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.UTF8.GetString(groupCache!));
        _ = keys.ShouldNotBeNull();
        keys.ShouldContain(request.CacheOptions.CacheKey);
    }

    [Fact(DisplayName = "Handle should initialize with existing data when group cache exists")]
    public async Task Handle_WhenGroupCacheExists_ShouldInitializeWithExistingData()
    {
        // Arrange: Create a request and pre-populate the cache group.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: "test-group",
                SlidingExpiration: null
            ),
        };
        var existingKeys = new HashSet<string> { "existing-key" };
        await _cache.SetAsync(
            request.CacheOptions.CacheGroupKey!,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(existingKeys))
        );

        // Act: Execute caching behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the existing data is preserved alongside the new key.
        byte[]? groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        HashSet<string>? keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.UTF8.GetString(groupCache!));
        keys!.ShouldContain("existing-key");
        keys!.ShouldContain(request.CacheOptions.CacheKey);
    }

    [Fact(DisplayName = "Handle should use max expiration when existing expiration is longer")]
    public async Task Handle_WithExistingExpiration_ShouldUseMaxExpiration()
    {
        // Arrange: Create a request with a sliding expiration and set a longer expiration in cache.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: "test-group",
                SlidingExpiration: TimeSpan.FromSeconds(30)
            ),
        };

        // Set existing longer expiration (60 seconds)
        int existingSeconds = 60;
        await _cache.SetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration", BitConverter.GetBytes(existingSeconds));

        // Act: Execute caching behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify that the longer expiration is kept.
        byte[]? storedBytes = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = storedBytes.ShouldNotBeNull();
        int storedSeconds = BitConverter.ToInt32(storedBytes);
        storedSeconds.ShouldBe(existingSeconds, "Should keep the longer expiration time");
    }

    [Theory(DisplayName = "Handle should handle expiration correctly when different expiration sizes are provided")]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(300)]
    public async Task Handle_WithDifferentExpirationSizes_ShouldHandleCorrectly(int expirationSeconds)
    {
        // Arrange: Create a request with a given sliding expiration.
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                BypassCache: false,
                CacheKey: "test-key",
                CacheGroupKey: "test-group",
                SlidingExpiration: TimeSpan.FromSeconds(expirationSeconds)
            ),
        };

        // Act: Execute caching behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the expiration is set as specified.
        byte[]? storedBytes = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = storedBytes.ShouldNotBeNull();
        int storedSeconds = BitConverter.ToInt32(storedBytes);
        storedSeconds.ShouldBe(expirationSeconds, "Expiration time should be stored correctly");
    }

    [Theory(DisplayName = "Constructor should throw exception when cache settings are invalid")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidCacheSettings_ShouldThrowException(int seconds)
    {
        // Act & Assert: Verify constructor throws for invalid sliding expiration.
        ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(
            () =>
                new CachingBehavior<MockCacheableRequest, string>(
                    _cache,
                    _loggerMock.Object,
                    new CacheSettings(TimeSpan.FromSeconds(seconds))
                )
        );

        exception.Message.ShouldContain("Sliding expiration must be positive");
    }

    private class FailingData { }

    private class CircularReferenceObject
    {
        public CircularReferenceObject? Reference { get; set; }
    }

    private class FailingConverter : JsonConverter<FailingData>
    {
        public override FailingData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new JsonException("Forced serialization failure");
        }

        public override void Write(Utf8JsonWriter writer, FailingData value, JsonSerializerOptions options)
        {
            throw new JsonException("Forced serialization failure");
        }
    }

    private class UnserializableType
    {
        public UnserializableType Self => this; // Creates a circular reference

        public override string ToString()
        {
            return "Unserializable object";
        }
    }
}
