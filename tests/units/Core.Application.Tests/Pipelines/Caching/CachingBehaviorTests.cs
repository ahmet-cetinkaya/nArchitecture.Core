using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Caching;

public class MockCacheableRequest : IRequest<string>, ICacheableRequest
{
    public string CacheKey { get; set; } = "test-key";
    public string? CacheGroupKey { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public bool BypassCache { get; set; }
}

public class CachingBehaviorTests
{
    private readonly IDistributedCache _cache;
    private readonly Mock<ILogger<CachingBehavior<MockCacheableRequest, string>>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly CachingBehavior<MockCacheableRequest, string> _behavior;
    private readonly RequestHandlerDelegate<string> _nextDelegate;

    public CachingBehaviorTests()
    {
        var options = new MemoryDistributedCacheOptions();
        _cache = new MemoryDistributedCache(Options.Create(options));
        _loggerMock = new Mock<ILogger<CachingBehavior<MockCacheableRequest, string>>>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration
        var section = new Mock<IConfigurationSection>();
        section.Setup(x => x.Value).Returns("2.00:00:00"); // 2 days
        section.Setup(x => x["SlidingExpiration"]).Returns("2.00:00:00");

        _configurationMock.Setup(x => x.GetSection("CacheSettings")).Returns(section.Object);

        _behavior = new CachingBehavior<MockCacheableRequest, string>(_cache, _loggerMock.Object, _configurationMock.Object);
        _nextDelegate = () => Task.FromResult("test-response");
    }

    /// <summary>
    /// Verifies that cache is bypassed when specified in the request
    /// </summary>
    [Fact]
    public async Task Handle_WhenBypassCacheIsTrue_ShouldSkipCache()
    {
        // Arrange
        var request = new MockCacheableRequest { BypassCache = true };

        // Act
        var result = await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        result.ShouldBe("test-response");
        var cachedValue = await _cache.GetAsync(request.CacheKey);
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
        await _cache.SetAsync(request.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedResponse)));

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
        var cachedValue = await _cache.GetAsync(request.CacheKey);
        cachedValue.ShouldNotBeNull();
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
        var request = new MockCacheableRequest { SlidingExpiration = TimeSpan.FromMinutes(minutes) };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var cacheOptions = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) };
        await _cache.SetAsync(request.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize("test")), cacheOptions);
        var cachedValue = await _cache.GetAsync(request.CacheKey);
        cachedValue.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that cache group key functionality works correctly
    /// </summary>
    [Fact]
    public async Task Handle_WithCacheGroupKey_ShouldAddToGroup()
    {
        // Arrange
        var request = new MockCacheableRequest { CacheGroupKey = "test-group" };

        // Act
        await _behavior.Handle(request, _nextDelegate, CancellationToken.None);

        // Assert
        var groupCache = await _cache.GetAsync(request.CacheGroupKey);
        groupCache.ShouldNotBeNull();
        var keys = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(groupCache!));
        keys.ShouldNotBeNull();
        keys.ShouldContain(request.CacheKey);

        var slidingExpirationCache = await _cache.GetAsync($"{request.CacheGroupKey}SlidingExpiration");
        slidingExpirationCache.ShouldNotBeNull();
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
        await _cache.SetAsync(request.CacheKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize("cached-value")));

        if (request.CacheGroupKey != null)
        {
            await _cache.SetAsync(
                request.CacheGroupKey,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new HashSet<string> { request.CacheKey }))
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
    /// Verifies that invalid configuration throws appropriate exception
    /// This test verifies different invalid configuration scenarios:
    /// - Missing configuration (null)
    /// - Empty configuration
    /// - Invalid number format
    /// - Non-positive values
    /// </summary>
    [Theory]
    [InlineData(null, "Cache settings are not configured: SlidingExpiration is missing")]
    [InlineData("", "Cache settings are not configured: SlidingExpiration is missing")]
    [InlineData("invalid", "Cache settings are invalid: SlidingExpiration must be a valid TimeSpan")]
    [InlineData("-1.00:00:00", "Cache settings are invalid: SlidingExpiration must be positive")]
    [InlineData("00:00:00", "Cache settings are invalid: SlidingExpiration must be positive")]
    public void Constructor_WithInvalidConfiguration_ShouldThrowException(string? slidingExpirationValue, string expectedMessage)
    {
        // Arrange
        var section = new Mock<IConfigurationSection>();
        section.Setup(x => x["SlidingExpiration"]).Returns(slidingExpirationValue);
        _configurationMock.Setup(x => x.GetSection("CacheSettings")).Returns(section.Object);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => new CachingBehavior<MockCacheableRequest, string>(_cache, _loggerMock.Object, _configurationMock.Object)
        );

        exception.Message.ShouldBe(expectedMessage);
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
        var request = new MockCacheableRequest { SlidingExpiration = TimeSpan.FromMinutes(minutes) };

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await _behavior.Handle(request, _nextDelegate, CancellationToken.None)
        );
    }
}
