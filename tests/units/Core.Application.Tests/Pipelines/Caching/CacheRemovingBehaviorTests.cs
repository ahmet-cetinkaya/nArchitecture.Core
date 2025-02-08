using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Caching;

public class MockCacheRemoverRequest : IRequest<int>, ICacheRemoverRequest
{
    // Use a backing property for the new CacheOptions
    public CacheRemoverOptions CacheOptions { get; set; } =
        new CacheRemoverOptions(bypassCache: false, cacheKey: string.Empty, cacheGroupKey: System.Array.Empty<string>());
}

public class CacheRemovingBehaviorTests
{
    private readonly IDistributedCache _cache;
    private readonly Mock<ILogger<CacheRemovingBehavior<MockCacheRemoverRequest, int>>> _loggerMock;
    private readonly CacheRemovingBehavior<MockCacheRemoverRequest, int> _behavior;
    private readonly RequestHandlerDelegate<int> _nextDelegate;

    public CacheRemovingBehaviorTests()
    {
        var options = new MemoryDistributedCacheOptions();
        _cache = new MemoryDistributedCache(Options.Create(options));
        _loggerMock = new Mock<ILogger<CacheRemovingBehavior<MockCacheRemoverRequest, int>>>();
        _behavior = new CacheRemovingBehavior<MockCacheRemoverRequest, int>(_cache, _loggerMock.Object);
        _nextDelegate = () => Task.FromResult(42);
    }

    /// <summary>
    /// Verifies that a specific cache key is successfully removed when provided in the request.
    /// This test ensures the basic cache removal functionality works for single keys.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCacheKeyProvided_ShouldRemoveFromCache()
    {
        // Arrange
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: false, cacheKey: "test-key", cacheGroupKey: null),
        };
        await _cache.SetAsync("test-key", Encoding.UTF8.GetBytes("test-value"));

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var result = await _cache.GetAsync("test-key");
        result.ShouldBeNull("Cache key should be removed from cache");
    }

    /// <summary>
    /// Validates that when a group key is provided, all associated cache entries are removed,
    /// including the group metadata and sliding expiration information.
    /// This test ensures proper cleanup of group-based caching.
    /// </summary>
    [Fact]
    public async Task Handle_WhenGroupKeyProvided_ShouldRemoveEntireGroup()
    {
        // Arrange
        var groupKey = "group1";
        var cachedKeys = new HashSet<string> { "key1", "key2" };
        var serializedKeys = JsonSerializer.Serialize(cachedKeys);

        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(serializedKeys));
        foreach (var key in cachedKeys)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }
        await _cache.SetAsync($"{groupKey}SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: false, cacheKey: null, cacheGroupKey: new[] { groupKey }),
        };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        foreach (var key in cachedKeys)
        {
            var value = await _cache.GetAsync(key);
            value.ShouldBeNull($"Cache key '{key}' should be removed");
        }

        var groupValue = await _cache.GetAsync(groupKey);
        groupValue.ShouldBeNull("Group key should be removed");

        var slidingValue = await _cache.GetAsync($"{groupKey}SlidingExpiration");
        slidingValue.ShouldBeNull("Sliding expiration key should be removed");
    }

    /// <summary>
    /// Tests the behavior when multiple cache groups need to be removed.
    /// Verifies that all cache entries, group metadata, and sliding expiration data
    /// are properly removed for each specified group.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMultipleGroupKeysProvided_ShouldRemoveAllGroups()
    {
        // Arrange
        var groupKeys = new[] { "group1", "group2" };
        var cachedKeys1 = new HashSet<string> { "key1", "key2" };
        var cachedKeys2 = new HashSet<string> { "key3", "key4" };

        // Setup first group
        await _cache.SetAsync("group1", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedKeys1)));
        foreach (var key in cachedKeys1)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }
        await _cache.SetAsync("group1SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        // Setup second group
        await _cache.SetAsync("group2", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedKeys2)));
        foreach (var key in cachedKeys2)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }
        await _cache.SetAsync("group2SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: false, cacheKey: null, cacheGroupKey: groupKeys),
        };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        foreach (var key in cachedKeys1.Concat(cachedKeys2))
        {
            var value = await _cache.GetAsync(key);
            value.ShouldBeNull($"Cache key '{key}' should be removed");
        }

        foreach (var groupKey in groupKeys)
        {
            var groupValue = await _cache.GetAsync(groupKey);
            groupValue.ShouldBeNull($"Group key '{groupKey}' should be removed");

            var slidingValue = await _cache.GetAsync($"{groupKey}SlidingExpiration");
            slidingValue.ShouldBeNull($"Sliding expiration key for '{groupKey}' should be removed");
        }
    }

    /// <summary>
    /// Ensures the behavior handles non-existent group keys gracefully without affecting
    /// other cache entries. This test verifies the system's resilience when dealing
    /// with missing cache groups.
    /// </summary>
    [Fact]
    public async Task Handle_WhenGroupKeyDoesNotExist_ShouldHandleGracefully()
    {
        // Arrange
        var existingKey = "existing-key";
        await _cache.SetAsync(existingKey, Encoding.UTF8.GetBytes("test-value"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                bypassCache: false,
                cacheKey: null,
                cacheGroupKey: new[] { "non-existent-group" }
            ),
        };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var existingValue = await _cache.GetAsync(existingKey);
        existingValue.ShouldNotBeNull("Existing cache entries should not be affected");
    }

    /// <summary>
    /// Verifies that when BypassCache is set to true, no cache operations are performed
    /// regardless of the provided cache keys or group keys. This test ensures the bypass
    /// functionality works as expected.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBypassCacheIsTrue_ShouldSkipCacheOperations()
    {
        // Arrange
        var testKey = "test-key";
        var groupKey = "group1";
        await _cache.SetAsync(testKey, Encoding.UTF8.GetBytes("test-value"));
        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { testKey })));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: true, cacheKey: testKey, cacheGroupKey: new[] { groupKey }),
        };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var value = await _cache.GetAsync(testKey);
        value.ShouldNotBeNull("Cache should not be modified when bypassing cache");

        var groupValue = await _cache.GetAsync(groupKey);
        groupValue.ShouldNotBeNull("Group cache should not be modified when bypassing cache");
    }

    /// <summary>
    /// Confirms that the next delegate in the pipeline is always called regardless
    /// of cache operations. This ensures the behavior doesn't break the request pipeline.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldAlwaysCallNextDelegate()
    {
        // Arrange
        bool nextDelegateCalled = false;
        var request = new MockCacheRemoverRequest();
        RequestHandlerDelegate<int> next = () =>
        {
            nextDelegateCalled = true;
            return Task.FromResult(42);
        };

        // Act
        await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextDelegateCalled.ShouldBeTrue("Next delegate should always be called");
    }

    /// <summary>
    /// Tests various combinations of cache configurations to ensure proper behavior
    /// in different scenarios. This parameterized test covers multiple use cases:
    /// - No cache key or group keys
    /// - Only cache key
    /// - Only group keys
    /// - Both cache key and group keys
    /// - Bypass cache with both types of keys
    /// </summary>
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("test-key", null, false)]
    [InlineData(null, new[] { "group1" }, false)]
    [InlineData("test-key", new[] { "group1" }, false)]
    [InlineData("test-key", new[] { "group1" }, true)]
    public async Task Handle_WithDifferentRequestConfigurations_ShouldBehaveCorrectly(
        string? cacheKey,
        string[]? groupKeys,
        bool bypassCache
    )
    {
        // Arrange
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: bypassCache, cacheKey: cacheKey, cacheGroupKey: groupKeys),
        };

        if (cacheKey != null)
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes("test-value"));

        if (groupKeys != null)
            foreach (var groupKey in groupKeys)
            {
                var groupData = new HashSet<string> { "key1" };
                await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
                await _cache.SetAsync("key1", Encoding.UTF8.GetBytes("value1"));
            }

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe(42);

        if (!bypassCache)
        {
            if (cacheKey != null)
            {
                var cachedValue = await _cache.GetAsync(cacheKey);
                cachedValue.ShouldBeNull();
            }

            if (groupKeys != null)
            {
                foreach (var groupKey in groupKeys)
                {
                    var groupValue = await _cache.GetAsync(groupKey);
                    groupValue.ShouldBeNull();
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the behavior handles empty group key arrays without throwing exceptions.
    /// This edge case test ensures system stability when provided with empty collections.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyGroupKey_ShouldNotThrowException()
    {
        // Arrange
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: false, cacheKey: null, cacheGroupKey: Array.Empty<string>()),
        };

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _nextDelegate, CancellationToken.None));

        // Assert
        exception.ShouldBeNull();
    }

    /// <summary>
    /// Tests the behavior's handling of corrupted or invalid JSON data in group cache entries.
    /// Ensures the system fails gracefully and throws appropriate exceptions when
    /// encountering malformed cache data.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidGroupKeyData_ShouldHandleGracefully()
    {
        // Arrange
        const string groupKey = "invalid-group";
        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes("invalid-json"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(bypassCache: false, cacheKey: null, cacheGroupKey: new[] { groupKey }),
        };

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _nextDelegate, CancellationToken.None));

        // Assert
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<JsonException>();
    }

    /// <summary>
    /// Verifies that the behavior properly respects cancellation tokens and
    /// terminates operations when cancellation is requested. This ensures
    /// the system remains responsive and can be interrupted when needed.
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                bypassCache: false,
                cacheKey: "test-key",
                cacheGroupKey: new[] { "test-group" }
            ),
        };

        // Set up some cache data to ensure we hit the cache operations
        await _cache.SetAsync("test-group", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new HashSet<string> { "test-key" })));
        await _cache.SetAsync("test-key", Encoding.UTF8.GetBytes("test-value"));

        cts.Cancel(); // Cancel before execution

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _behavior.Handle(request, _nextDelegate, cts.Token);
        });
    }

    /// <summary>
    /// Tests that cancellation is properly handled during group key processing
    /// </summary>
    [Fact]
    public async Task Handle_WithCancellationDuringGroupKeyProcessing_ShouldStopProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                bypassCache: false,
                cacheKey: null,
                cacheGroupKey: new[] { "group1", "group2", "group3" } // Multiple groups to ensure we hit the loop
            ),
        };

        // Set up cache data
        var groupData = new HashSet<string> { "key1" };
        foreach (var groupKey in request.CacheOptions.CacheGroupKey!)
        {
            await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
        }

        // Set up next delegate to cancel during execution
        RequestHandlerDelegate<int> next = () =>
        {
            cts.Cancel(); // Cancel during execution
            return Task.FromResult(42);
        };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _behavior.Handle(request, next, cts.Token);
        });

        // Verify that not all groups were processed
        var lastGroupValue = await _cache.GetAsync("group3");
        lastGroupValue.ShouldNotBeNull("Processing should have stopped before reaching the last group");
    }

    /// <summary>
    /// Tests cancellation handling during group processing loop
    /// </summary>
    [Fact]
    public async Task Handle_WhenCancellationRequestedDuringLoop_ShouldBreakAndStopProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                bypassCache: false,
                cacheKey: null,
                cacheGroupKey: new[] { "group1", "group2", "group3", "group4" }
            ),
        };

        // Setup cache data
        foreach (var groupKey in request.CacheOptions.CacheGroupKey!)
        {
            var groupData = new HashSet<string> { "testKey" };
            await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
        }

        // Cancel after first group is processed
        int processedGroups = 0;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        _loggerMock
            .Setup(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                )
            )
            .Callback(() =>
            {
                processedGroups++;
                if (processedGroups == 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException(cts.Token);
                }
            });

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _behavior.Handle(request, _nextDelegate, cts.Token);
        });

        // Verify only first group was processed
        (await _cache.GetAsync("group1")).ShouldBeNull("First group should be processed");
        (await _cache.GetAsync("group2")).ShouldNotBeNull("Second group should not be processed");
        (await _cache.GetAsync("group3")).ShouldNotBeNull("Third group should not be processed");
        (await _cache.GetAsync("group4")).ShouldNotBeNull("Fourth group should not be processed");
    }

    /// <summary>
    /// Tests that logger information is properly called for each group
    /// </summary>
    [Fact]
    public async Task Handle_WhenProcessingGroups_ShouldLogInformation()
    {
        // Arrange
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                bypassCache: false,
                cacheKey: null,
                cacheGroupKey: new[] { "group1", "group2" }
            ),
        };

        // Setup cache data with different number of keys per group
        var group1Keys = new HashSet<string> { "key1", "key2" };
        var group2Keys = new HashSet<string> { "key3" };
        await _cache.SetAsync("group1", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group1Keys)));
        await _cache.SetAsync("group2", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group2Keys)));

        // Enable information logging
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        // Verify logger was called with correct group names and key counts
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("group1") && v.ToString()!.Contains("2")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("group2") && v.ToString()!.Contains("1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
