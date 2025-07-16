using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Caching;

public class MockCacheRemoverRequest : IRequest<int>, ICacheRemoverRequest
{
    public CacheRemoverOptions CacheOptions { get; set; } =
        new CacheRemoverOptions(BypassCache: false, CacheKey: string.Empty, CacheGroupKey: []);
}

[Trait("Category", "CacheRemoving")]
public class CacheRemovingBehaviorTests
{
    private readonly IDistributedCache _cache;
    private readonly Mock<ILogger> _loggerMock;
    private readonly CacheRemovingBehavior<MockCacheRemoverRequest, int> _behavior;
    private readonly RequestHandlerDelegate<int> _nextDelegate;

    public CacheRemovingBehaviorTests()
    {
        var options = new MemoryDistributedCacheOptions();
        _cache = new MemoryDistributedCache(Options.Create(options));
        _loggerMock = new Mock<ILogger>();
        _behavior = new CacheRemovingBehavior<MockCacheRemoverRequest, int>(_cache, _loggerMock.Object);
        _nextDelegate = () => Task.FromResult(42);
    }

    [Fact(DisplayName = "Handle should remove from cache when cache key is provided")]
    public async Task Handle_WhenCacheKeyProvided_ShouldRemoveFromCache()
    {
        // Arrange: Create a request with a specific cache key.
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: "test-key", CacheGroupKey: null),
        };
        await _cache.SetAsync("test-key", Encoding.UTF8.GetBytes("test-value"));

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the cache key is removed.
        byte[]? result = await _cache.GetAsync("test-key");
        result.ShouldBeNull("Cache key should be removed from cache");
    }

    [Fact(DisplayName = "Handle should remove entire group when group key is provided")]
    public async Task Handle_WhenGroupKeyProvided_ShouldRemoveEntireGroup()
    {
        // Arrange: Setup a cache group with keys.
        string groupKey = "group1";
        var cachedKeys = new HashSet<string> { "key1", "key2" };
        string serializedKeys = JsonSerializer.Serialize(cachedKeys);

        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(serializedKeys));
        foreach (string key in cachedKeys)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }

        await _cache.SetAsync($"{groupKey}SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: [groupKey]),
        };

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify all group keys and metadata are removed.
        foreach (string key in cachedKeys)
        {
            byte[]? value = await _cache.GetAsync(key);
            value.ShouldBeNull($"Cache key '{key}' should be removed");
        }

        byte[]? groupValue = await _cache.GetAsync(groupKey);
        groupValue.ShouldBeNull("Group key should be removed");

        byte[]? slidingValue = await _cache.GetAsync($"{groupKey}SlidingExpiration");
        slidingValue.ShouldBeNull("Sliding expiration key should be removed");
    }

    [Fact(DisplayName = "Handle should remove all groups when multiple group keys are provided")]
    public async Task Handle_WhenMultipleGroupKeysProvided_ShouldRemoveAllGroups()
    {
        // Arrange: Create a request with multiple cache group keys.
        string[] groupKeys = ["group1", "group2"];
        var cachedKeys1 = new HashSet<string> { "key1", "key2" };
        var cachedKeys2 = new HashSet<string> { "key3", "key4" };

        // Setup first group
        await _cache.SetAsync("group1", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedKeys1)));
        foreach (string key in cachedKeys1)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }
        await _cache.SetAsync("group1SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        // Setup second group
        await _cache.SetAsync("group2", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedKeys2)));
        foreach (string key in cachedKeys2)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes($"value-{key}"));
        }
        await _cache.SetAsync("group2SlidingExpiration", Encoding.UTF8.GetBytes("30"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: groupKeys),
        };

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify all keys across groups are removed.
        foreach (string key in cachedKeys1.Concat(cachedKeys2))
        {
            byte[]? value = await _cache.GetAsync(key);
            value.ShouldBeNull($"Cache key '{key}' should be removed");
        }

        foreach (string groupKey in groupKeys)
        {
            byte[]? groupValue = await _cache.GetAsync(groupKey);
            groupValue.ShouldBeNull($"Group key '{groupKey}' should be removed");

            byte[]? slidingValue = await _cache.GetAsync($"{groupKey}SlidingExpiration");
            slidingValue.ShouldBeNull($"Sliding expiration key for '{groupKey}' should be removed");
        }
    }

    [Fact(DisplayName = "Handle should handle gracefully when group key does not exist")]
    public async Task Handle_WhenGroupKeyDoesNotExist_ShouldHandleGracefully()
    {
        // Arrange: Create a request with a non-existent cache group.
        string existingKey = "existing-key";
        await _cache.SetAsync(existingKey, Encoding.UTF8.GetBytes("test-value"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: ["non-existent-group"]),
        };

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify existing keys are unaffected.
        byte[]? existingValue = await _cache.GetAsync(existingKey);
        _ = existingValue.ShouldNotBeNull("Existing cache entries should not be affected");
    }

    [Fact(DisplayName = "Handle should skip cache operations when bypass cache is true")]
    public async Task Handle_WhenBypassCacheIsTrue_ShouldSkipCacheOperations()
    {
        // Arrange: Create request with bypass flag.
        string testKey = "test-key";
        string groupKey = "group1";
        await _cache.SetAsync(testKey, Encoding.UTF8.GetBytes("test-value"));
        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { testKey })));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: true, CacheKey: testKey, CacheGroupKey: [groupKey]),
        };

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify cache remains unchanged.
        byte[]? value = await _cache.GetAsync(testKey);
        _ = value.ShouldNotBeNull("Cache should not be modified when bypassing cache");

        byte[]? groupValue = await _cache.GetAsync(groupKey);
        _ = groupValue.ShouldNotBeNull("Group cache should not be modified when bypassing cache");
    }

    [Fact(DisplayName = "Handle should always call next delegate")]
    public async Task Handle_ShouldAlwaysCallNextDelegate()
    {
        // Arrange: Prepare a request and a next delegate.
        bool nextDelegateCalled = false;
        var request = new MockCacheRemoverRequest();
        Task<int> next()
        {
            nextDelegateCalled = true;
            return Task.FromResult(42);
        }

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert: Verify next delegate is invoked.
        nextDelegateCalled.ShouldBeTrue("Next delegate should always be called");
    }

    [Theory(DisplayName = "Handle should behave correctly when different request configurations are provided")]
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
        // Arrange: Create request with provided configurations.
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: bypassCache, CacheKey: cacheKey, CacheGroupKey: groupKeys),
        };

        if (cacheKey != null)
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes("test-value"));

        if (groupKeys != null)
        {
            foreach (string groupKey in groupKeys)
            {
                var groupData = new HashSet<string> { "key1" };
                await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
                await _cache.SetAsync("key1", Encoding.UTF8.GetBytes("value1"));
            }
        }

        // Act: Execute cache removal behavior.
        int result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify the behavior returns expected result and cache is modified accordingly.
        result.ShouldBe(42);

        if (!bypassCache)
        {
            if (cacheKey != null)
            {
                byte[]? cachedValue = await _cache.GetAsync(cacheKey);
                cachedValue.ShouldBeNull();
            }

            if (groupKeys != null)
            {
                foreach (string groupKey in groupKeys)
                {
                    byte[]? groupValue = await _cache.GetAsync(groupKey);
                    groupValue.ShouldBeNull();
                }
            }
        }
    }

    [Fact(DisplayName = "Handle should not throw exception when group key is empty")]
    public async Task Handle_WithEmptyGroupKey_ShouldNotThrowException()
    {
        // Arrange: Create a request with empty group key.
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: []),
        };

        // Act: Execute cache removal behavior.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _nextDelegate, CancellationToken.None));

        // Assert: Verify no exception is thrown.
        exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Handle should handle gracefully when group key data is invalid")]
    public async Task Handle_WithInvalidGroupKeyData_ShouldHandleGracefully()
    {
        // Arrange: Setup cache with invalid group key data.
        const string groupKey = "invalid-group";
        await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes("invalid-json"));

        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: [groupKey]),
        };

        // Act: Execute cache removal behavior.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _nextDelegate, CancellationToken.None));

        // Assert: Verify that an appropriate exception is raised.
        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<JsonException>();
    }

    [Fact(DisplayName = "Handle should respect cancellation token when cancellation is requested")]
    public async Task Handle_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange: Create request and cancel token.
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: "test-key", CacheGroupKey: ["test-group"]),
        };

        await _cache.SetAsync("test-group", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new HashSet<string> { "test-key" })));
        await _cache.SetAsync("test-key", Encoding.UTF8.GetBytes("test-value"));

        cts.Cancel();

        // Act & Assert: Verify OperationCanceledException is thrown.
        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            _ = await _behavior.Handle(request, _nextDelegate, cts.Token);
        });
    }

    [Fact(DisplayName = "Handle should stop processing when cancellation occurs during group processing")]
    public async Task Handle_WithCancellationDuringGroupKeyProcessing_ShouldStopProcessing()
    {
        // Arrange: Configure cache groups and cancellation.
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                BypassCache: false,
                CacheKey: null,
                CacheGroupKey: ["group1", "group2", "group3"]
            ),
        };

        var groupData = new HashSet<string> { "key1" };
        foreach (string groupKey in request.CacheOptions.CacheGroupKey!)
        {
            await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
        }

        Task<int> next()
        {
            cts.Cancel();
            return Task.FromResult(42);
        }

        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            _ = await _behavior.Handle(request, next, cts.Token);
        });

        byte[]? lastGroupValue = await _cache.GetAsync("group3");
        _ = lastGroupValue.ShouldNotBeNull("Processing should have stopped before reaching the last group");
    }

    [Fact(DisplayName = "Handle should break and stop processing when cancellation is requested during loop")]
    public async Task Handle_WhenCancellationRequestedDuringLoop_ShouldBreakAndStopProcessing()
    {
        // Arrange: Prepare multiple groups and a cancellation token.
        var cts = new CancellationTokenSource();
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(
                BypassCache: false,
                CacheKey: null,
                CacheGroupKey: ["group1", "group2", "group3", "group4"]
            ),
        };

        foreach (string groupKey in request.CacheOptions.CacheGroupKey!)
        {
            var groupData = new HashSet<string> { "testKey" };
            await _cache.SetAsync(groupKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(groupData)));
        }

        int processedGroups = 0;
        _ = _loggerMock
            .Setup(x => x.InformationAsync(It.IsAny<string>()))
            .Callback(() =>
            {
                processedGroups++;
                if (processedGroups == 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException(cts.Token);
                }
            })
            .Returns(Task.CompletedTask);

        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            _ = await _behavior.Handle(request, _nextDelegate, cts.Token);
        });

        (await _cache.GetAsync("group1")).ShouldBeNull("First group should be processed");
        _ = (await _cache.GetAsync("group2")).ShouldNotBeNull("Second group should not be processed");
        _ = (await _cache.GetAsync("group3")).ShouldNotBeNull("Third group should not be processed");
        _ = (await _cache.GetAsync("group4")).ShouldNotBeNull("Fourth group should not be processed");
    }

    [Fact(DisplayName = "Handle should log information when processing groups")]
    public async Task Handle_WhenProcessingGroups_ShouldLogInformation()
    {
        // Arrange: Setup cache groups and mock logger.
        var request = new MockCacheRemoverRequest
        {
            CacheOptions = new CacheRemoverOptions(BypassCache: false, CacheKey: null, CacheGroupKey: ["group1", "group2"]),
        };

        var group1Keys = new HashSet<string> { "key1", "key2" };
        var group2Keys = new HashSet<string> { "key3" };
        await _cache.SetAsync("group1", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group1Keys)));
        await _cache.SetAsync("group2", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(group2Keys)));

        // Act: Execute cache removal behavior.
        _ = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert: Verify logger logs the correct information.
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(m => m.Contains("group1") && m.Contains("2"))), Times.Once);
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(m => m.Contains("group2") && m.Contains("1"))), Times.Once);
    }
}
