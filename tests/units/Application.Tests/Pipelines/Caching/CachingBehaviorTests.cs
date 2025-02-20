using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Caching;

public class MockCacheableRequest : IRequest<string>, ICacheableRequest
{
    public CacheableOptions CacheOptions { get; set; } =
        new CacheableOptions(bypassCache: false, cacheKey: "test-key", cacheGroupKey: null, slidingExpiration: null);
}

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

    /// <summary>
    /// Verifies that cache is bypassed when specified in the request
    /// </summary>
    [Fact]
    public async Task Handle_WhenBypassCacheIsTrue_ShouldSkipCache()
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: true,
                cacheKey: "test-key",
                cacheGroupKey: null,
                slidingExpiration: null
            ),
        };

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe("test-response");
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        cachedValue.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that response is retrieved from cache when available
    /// </summary>
    [Fact]
    public async Task Handle_WhenCacheExists_ShouldReturnCachedResponse()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var cachedResponse = "cached-response";
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedResponse)));

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe(cachedResponse);
    }

    /// <summary>
    /// Verifies that response is cached when not in cache
    /// </summary>
    [Fact]
    public async Task Handle_WhenCacheDoesNotExist_ShouldCacheResponse()
    {
        // Arrange
        var request = new MockCacheableRequest();

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe("test-response");
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        var cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse.ShouldBe("test-response");
    }

    /// <summary>
    /// Verifies that custom sliding expiration is respected
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Handle_WithCustomSlidingExpiration_ShouldRespectExpiration(int minutes)
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: null,
                slidingExpiration: TimeSpan.FromMinutes(minutes)
            ),
        };

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var cacheOptions = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) };
        await _cache.SetAsync(
            request.CacheOptions.CacheKey,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize("test")),
            cacheOptions
        );
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that cache group key functionality works correctly
    /// </summary>
    [Fact]
    public async Task Handle_WithCacheGroupKey_ShouldAddToGroup()
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: "test-group",
                slidingExpiration: null
            ),
        };

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        _ = groupCache.ShouldNotBeNull();
        var keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(groupCache!));
        _ = keys.ShouldNotBeNull();
        keys.ShouldContain(request.CacheOptions.CacheKey);

        var slidingExpirationCache = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = slidingExpirationCache.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies handling of cancellation requests
    /// </summary>
    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldCancelOperation()
    {
        // Arrange
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

        // Act & Assert
        var exception = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _behavior.Handle(request, _nextDelegate, cts.Token)
        );
    }

    /// <summary>
    /// Verifies that invalid sliding expiration values are handled properly
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_WithInvalidSlidingExpiration_ShouldThrowException(int minutes)
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: null,
                slidingExpiration: TimeSpan.FromMinutes(minutes)
            ),
        };

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await _behavior.Handle(request, _nextDelegate, CancellationToken.None)
        );
    }

    /// <summary>
    /// Verifies behavior when cache deserialization fails
    /// </summary>
    [Fact]
    public async Task Handle_WhenCacheDeserializationFails_ShouldLogWarningAndContinue()
    {
        // Arrange
        var request = new MockCacheableRequest();
        // Store invalid JSON data in cache to force deserialization failure
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes("invalid-json-data"));

        _ = _loggerMock
            .Setup(x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe("test-response", "Should fallback to next delegate when deserialization fails");

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))),
            Times.Once,
            "Should log warning when deserialization fails"
        );
    }

    /// <summary>
    /// Tests caching of large responses that require array resizing
    /// </summary>
    [Fact]
    public async Task Handle_WithLargeResponse_ShouldHandleArrayResizing()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var largeResponse = new string('x', 8192); // Create a string larger than default buffer size
        RequestHandlerDelegate<string> largeResponseDelegate = () => Task.FromResult(largeResponse);

        // Act
        var result = await _behavior.Handle(request, largeResponseDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe(largeResponse);
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        var cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse.ShouldBe(largeResponse);
    }

    /// <summary>
    /// Tests caching of multiple large responses with different sizes
    /// </summary>
    [Theory]
    [InlineData(2048)] // Smaller than default
    [InlineData(4096)] // Equal to default
    [InlineData(8192)] // Larger than default
    [InlineData(16384)] // Much larger than default
    public async Task Handle_WithVariousResponseSizes_ShouldHandleArrayResizing(int responseSize)
    {
        // Arrange
        var request = new MockCacheableRequest();
        var response = new string('x', responseSize);
        RequestHandlerDelegate<string> responseDelegate = () => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, responseDelegate, CancellationToken.None);

        // Assert
        result.Length.ShouldBe(responseSize);
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull();
        var cachedResponse = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(cachedValue!));
        cachedResponse!.Length.ShouldBe(responseSize);
    }

    /// <summary>
    /// Tests handling of serialization failures in cache operations
    /// </summary>
    [Fact]
    public async Task Handle_WhenSerializationFails_ShouldHandleAndReleaseResources()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var failingData = new FailingData();
        RequestHandlerDelegate<string> failingDelegate = () => Task.FromResult(JsonSerializer.Serialize(failingData));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, failingDelegate, CancellationToken.None));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<JsonException>();

        // Verify the cache wasn't modified
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        cachedValue.ShouldBeNull("Cache should not be modified when serialization fails");
    }

    /// <summary>
    /// Tests that resources are properly released even when serialization fails
    /// </summary>
    [Fact]
    public async Task Handle_WhenSerializationFails_ShouldNotLeakResources()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var failingData = new FailingData();
        RequestHandlerDelegate<string> failingDelegate = () => Task.FromResult(JsonSerializer.Serialize(failingData));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, failingDelegate, CancellationToken.None));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<JsonException>();

        // Additional requests should still work
        var validRequest = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "valid-key",
                cacheGroupKey: null,
                slidingExpiration: null
            ),
        };
        var result = await _behavior.Handle(validRequest, _nextDelegate, CancellationToken.None);
        result.ShouldBe("test-response", "Subsequent requests should work after serialization failure");
    }

    /// <summary>
    /// Tests array pool resource cleanup during serialization failures
    /// </summary>
    [Fact]
    public async Task Handle_WhenSerializationFailsDuringArrayOperation_ShouldReleasePooledArray()
    {
        // Arrange
        var request = new MockCacheableRequest();

        // Create a delegate that will trigger array allocation and then fail serialization
        var count = 0;
        RequestHandlerDelegate<string> failingDelegate = () =>
        {
            count++;
            if (count == 1)
            {
                var circularObject = new CircularReferenceObject();
                circularObject.Reference = circularObject; // Create circular reference
                return Task.FromResult(JsonSerializer.Serialize(circularObject)); // This will throw
            }
            return Task.FromResult("test-response");
        };

        // Act & Assert
        _ = await Should.ThrowAsync<JsonException>(
            async () => await _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        // Verify we can still use the cache system after failure
        var validRequest = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-after-failure",
                cacheGroupKey: null,
                slidingExpiration: null
            ),
        };
        var result = await _behavior.Handle(validRequest, _nextDelegate, CancellationToken.None);
        result.ShouldBe("test-response");
    }

    /// <summary>
    /// Tests handling of null group cache data in PooledSet
    /// </summary>
    [Fact]
    public async Task Handle_WithNullGroupCache_ShouldHandlePooledSetInitialization()
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: "test-group",
                slidingExpiration: null
            ),
        };

        // Don't set any cache data to ensure null group cache

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe("test-response");

        // Verify group was created
        var groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        _ = groupCache.ShouldNotBeNull();
        var keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.UTF8.GetString(groupCache!));
        _ = keys.ShouldNotBeNull();
        keys.ShouldContain(request.CacheOptions.CacheKey);
    }

    /// <summary>
    /// Class that will always fail JSON serialization
    /// </summary>
    [JsonConverter(typeof(FailingConverter))]
    private class FailingData { }

    /// <summary>
    /// Class to create circular reference object
    /// </summary>
    private class CircularReferenceObject
    {
        public CircularReferenceObject? Reference { get; set; }
    }

    /// <summary>
    /// JsonConverter that always throws JsonException
    /// </summary>
    private class FailingConverter : JsonConverter<FailingData>
    {
        public override FailingData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new JsonException("Forced serialization failure");

        public override void Write(Utf8JsonWriter writer, FailingData value, JsonSerializerOptions options) =>
            throw new JsonException("Forced serialization failure");
    }

    /// <summary>
    /// Helper class that will fail JSON serialization
    /// </summary>
    private class UnserializableType
    {
        public UnserializableType Self => this; // Creates a circular reference

        public override string ToString() => "Unserializable object";
    }

    /// <summary>
    /// Tests the proper cleanup of array pool resources during exceptions
    /// </summary>
    [Fact]
    public async Task Handle_WhenSerializationThrows_ShouldReturnArrayToPool()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var throwingData = new ThrowingSerializationData();
        RequestHandlerDelegate<string> throwingDelegate = () => Task.FromResult(throwingData.ToString()!);

        // Act & Assert - First call will throw
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.Handle(request, throwingDelegate, CancellationToken.None)
        );

        // Verify pool is still usable
        var validRequest = new MockCacheableRequest();
        var result = await _behavior.Handle(validRequest, _nextDelegate, CancellationToken.None);
        result.ShouldBe("test-response");
    }

    /// <summary>
    /// Tests cache group initialization with existing data
    /// </summary>
    [Fact]
    public async Task Handle_WhenGroupCacheExists_ShouldInitializeWithExistingData()
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: "test-group",
                slidingExpiration: null
            ),
        };
        var existingKeys = new HashSet<string> { "existing-key" };
        await _cache.SetAsync(
            request.CacheOptions.CacheGroupKey!,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(existingKeys))
        );

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var groupCache = await _cache.GetAsync(request.CacheOptions.CacheGroupKey!);
        var keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.UTF8.GetString(groupCache!));
        keys!.ShouldContain("existing-key");
        keys!.ShouldContain(request.CacheOptions.CacheKey);
    }

    /// <summary>
    /// Tests sliding expiration calculation with existing expiration
    /// </summary>
    [Fact]
    public async Task Handle_WithExistingExpiration_ShouldUseMaxExpiration()
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: "test-group",
                slidingExpiration: TimeSpan.FromSeconds(30)
            ),
        };

        // Set existing longer expiration (60 seconds)
        var existingSeconds = 60;
        await _cache.SetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration", BitConverter.GetBytes(existingSeconds));

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var storedBytes = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = storedBytes.ShouldNotBeNull();
        var storedSeconds = BitConverter.ToInt32(storedBytes);
        storedSeconds.ShouldBe(existingSeconds, "Should keep the longer expiration time");
    }

    /// <summary>
    /// Tests handling of small and large expiration values
    /// </summary>
    [Theory]
    [InlineData(10)] // Small value
    [InlineData(60)] // Medium value
    [InlineData(300)] // Large value
    public async Task Handle_WithDifferentExpirationSizes_ShouldHandleCorrectly(int expirationSeconds)
    {
        // Arrange
        var request = new MockCacheableRequest
        {
            CacheOptions = new CacheableOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: "test-group",
                slidingExpiration: TimeSpan.FromSeconds(expirationSeconds)
            ),
        };

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var storedBytes = await _cache.GetAsync($"{request.CacheOptions.CacheGroupKey}SlidingExpiration");
        _ = storedBytes.ShouldNotBeNull();
        var storedSeconds = BitConverter.ToInt32(storedBytes);
        storedSeconds.ShouldBe(expirationSeconds, "Expiration time should be stored correctly");
    }

    private class ThrowingSerializationData
    {
        public override string ToString()
        {
            throw new InvalidOperationException("Forced exception during serialization");
        }
    }

    /// <summary>
    /// Tests that array pool resources are properly returned when serialization throws
    /// </summary>
    [Fact]
    public async Task Handle_WhenPooledSerializationFails_ShouldReturnArrayToPool()
    {
        // Arrange
        var request = new MockCacheableRequest();

        // Create a response that will trigger the pooled array serialization and then fail
        var explodingResponse = new ExplodingResponse();
        RequestHandlerDelegate<string> failingDelegate = () => Task.FromResult(explodingResponse.ToString());

        // Act - First call should throw
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        // Arrange - Second attempt with valid data to verify pool is still usable
        var largeResponse = new string('x', 8192); // Force pool usage
        RequestHandlerDelegate<string> validDelegate = () => Task.FromResult(largeResponse);

        // Act - Second call should succeed
        var result = await _behavior.Handle(request, validDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe(largeResponse, "Pool should be usable after error");
        var cachedValue = await _cache.GetAsync(request.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull("Cache operation should succeed after pool error");
    }

    private class ExplodingResponse
    {
        public override string ToString()
        {
            // This will cause the serialization to fail after the array is rented
            throw new InvalidOperationException("Forced failure during serialization");
        }
    }

    /// <summary>
    /// Tests that array pool resources are properly returned when serialization fails mid-operation
    /// </summary>
    [Fact]
    public async Task Handle_WhenSerializationFailsMidway_ShouldReleaseResources()
    {
        // Arrange
        var request = new MockCacheableRequest();
        var response = new SerializationExplodingObject(8192); // Force array allocation with size
        RequestHandlerDelegate<string> failingDelegate = () => Task.FromResult(response.ToString());

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.Handle(request, failingDelegate, CancellationToken.None)
        );

        // Verify we can still use arrays from the pool
        var validRequest = new MockCacheableRequest();
        var largeData = new string('x', 8192);
        var result = await _behavior.Handle(validRequest, () => Task.FromResult(largeData), CancellationToken.None);
        result.ShouldBe(largeData, "Pool should be usable after error");

        var cachedValue = await _cache.GetAsync(validRequest.CacheOptions.CacheKey);
        _ = cachedValue.ShouldNotBeNull("Cache should work after pool error");
    }

    private class SerializationExplodingObject
    {
        private readonly byte[] _data;

        public SerializationExplodingObject(int size)
        {
            _data = new byte[size];
            Array.Fill(_data, (byte)'x');
        }

        public override string ToString()
        {
            // Force allocation then fail
            var temp = new string((char)_data[0], _data.Length);
            throw new InvalidOperationException("Forced failure after allocation");
        }
    }

    /// <summary>
    /// Tests that logger information is properly called
    /// </summary>
    [Fact]
    public async Task Handle_WhenCacheHit_ShouldLogInformation()
    {
        // Arrange
        var request = new MockCacheableRequest();
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize("cached-value")));

        _ = _loggerMock
            .Setup(x => x.InformationAsync(It.Is<string>(msg => msg.Contains("Cache hit"))))
            .Returns(Task.CompletedTask);

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(msg => msg.Contains("Cache hit"))), Times.Once);
    }

    /// <summary>
    /// Tests that logger warning is properly called on deserialization failure
    /// </summary>
    [Fact]
    public async Task Handle_WhenDeserializationFails_ShouldLogWarning()
    {
        // Arrange
        var request = new MockCacheableRequest();
        await _cache.SetAsync(request.CacheOptions.CacheKey, Encoding.UTF8.GetBytes("invalid-json"));

        _ = _loggerMock
            .Setup(x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))))
            .Returns(Task.CompletedTask);

        // Act
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        _loggerMock.Verify(x => x.WarningAsync(It.Is<string>(msg => msg.Contains("Cache deserialization failed"))), Times.Once);
    }

    /// <summary>
    /// Verifies that invalid CacheSettings throws appropriate exception
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidCacheSettings_ShouldThrowException(int seconds)
    {
        // Arrange & Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () =>
                new CachingBehavior<MockCacheableRequest, string>(
                    _cache,
                    _loggerMock.Object,
                    new CacheSettings(TimeSpan.FromSeconds(seconds))
                )
        );

        exception.Message.ShouldContain("Sliding expiration must be positive");
    }
}
