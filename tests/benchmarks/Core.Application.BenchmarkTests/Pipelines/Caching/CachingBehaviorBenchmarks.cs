using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;

namespace NArchitecture.Core.Application.Benchmarks.Pipelines.Caching;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CachingBehaviorBenchmarks
{
    private CachingBehavior<TestRequest, string> _behavior = null!;
    private TestRequest _request = null!;
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<ILogger<CachingBehavior<TestRequest, string>>> _loggerMock = null!;
    private readonly Consumer _consumer = new();

    [GlobalSetup]
    public void Setup()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<CachingBehavior<TestRequest, string>>>();

        // Setup cache hit scenario
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) => Encoding.UTF8.GetBytes("\"cached-response\""));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "CacheSettings:SlidingExpiration", "2:00:00" } })
            .Build();

        _behavior = new CachingBehavior<TestRequest, string>(_cacheMock.Object, _loggerMock.Object, configuration);
        _request = new TestRequest { CacheKey = "test-key" };
    }

    [Benchmark(Description = "Cache Miss - New Entry", OperationsPerInvoke = 1000)]
    public async Task HandleCacheMiss()
    {
        for (int i = 0; i < 1000; i++)
        {
            _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

            await _behavior.Handle(_request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None);
        }
    }

    [Benchmark(Description = "Cache Hit - Existing Entry")]
    public async Task HandleCacheHit()
    {
        _request.BypassCache = false;
        await _behavior.Handle(_request, () => Task.FromResult("cached-response"), CancellationToken.None);
    }

    [Benchmark(Description = "Cache Bypass")]
    public async Task HandleCacheBypass()
    {
        _request.BypassCache = true;
        await _behavior.Handle(_request, () => Task.FromResult("bypass-response"), CancellationToken.None);
    }

    [Benchmark(Description = "Parallel Cache Miss - 100 Requests")]
    public async Task HandleParallelCacheMiss()
    {
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var request = new TestRequest { CacheKey = $"test-key-{i}" };
            tasks.Add(_behavior.Handle(request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "Sequential Cache Miss - 100 Requests")]
    public async Task HandleSequentialCacheMiss()
    {
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        for (int i = 0; i < 100; i++)
        {
            var request = new TestRequest { CacheKey = $"test-key-{i}" };
            await _behavior.Handle(request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None);
        }
    }

    [Benchmark(Description = "Parallel Cache Hit - 100 Requests")]
    public async Task HandleParallelCacheHit()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var request = new TestRequest { CacheKey = $"test-key-{i}" };
            tasks.Add(_behavior.Handle(request, () => Task.FromResult("cached-response"), CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "Sequential Cache Hit - 100 Requests")]
    public async Task HandleSequentialCacheHit()
    {
        for (int i = 0; i < 100; i++)
        {
            var request = new TestRequest { CacheKey = $"test-key-{i}" };
            await _behavior.Handle(request, () => Task.FromResult("cached-response"), CancellationToken.None);
        }
    }

    public class TestRequest : IRequest<string>, ICacheableRequest
    {
        public bool BypassCache { get; set; }
        public string CacheKey { get; set; } = string.Empty;
        public string? CacheGroupKey { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
    }
}
