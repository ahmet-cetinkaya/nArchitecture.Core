using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;

namespace NArchitecture.Core.Application.BenchmarkTests.Pipelines.Caching;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CacheRemovingBehaviorBenchmarks
{
    private CacheRemovingBehavior<TestRequest, string> _behavior = null!;
    private TestRequest _request = null!;
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<ILogger> _loggerMock = null!;
    private readonly Consumer _consumer = new();

    [GlobalSetup]
    public void Setup()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger>();

        _ = _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("[]"));

        _behavior = new CacheRemovingBehavior<TestRequest, string>(_cacheMock.Object, _loggerMock.Object);

        _request = new TestRequest { CacheKey = "test-key", CacheGroupKey = new[] { "group1", "group2" } };
    }

    [Benchmark(Baseline = true, Description = "Baseline - No Cache Operations")]
    public async ValueTask BaselineOperation()
    {
        for (int i = 0; i < 1000; i++)
            _ = await Task.FromResult($"test-response-{i}");
    }

    [Benchmark(Description = "Remove Single Key", OperationsPerInvoke = 1000)]
    public async ValueTask RemoveSingleKey()
    {
        for (int i = 0; i < 1000; i++) // Add loop to increase operation time
        {
            _request.CacheGroupKey = null;
            _ = await _behavior.Handle(_request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None);
        }
    }

    [Benchmark(Description = "Remove Group Keys", OperationsPerInvoke = 1000)]
    public async ValueTask RemoveGroupKeys()
    {
        for (int i = 0; i < 1000; i++)
        {
            _request.CacheKey = null;
            _ = await _behavior.Handle(_request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None);
        }
    }

    [Benchmark(Description = "Remove Both Single and Group", OperationsPerInvoke = 1000)]
    public async ValueTask RemoveBoth()
    {
        for (int i = 0; i < 1000; i++)
            _ = await _behavior.Handle(_request, () => Task.FromResult($"test-response-{i}"), CancellationToken.None);
    }

    public class TestRequest : IRequest<string>, ICacheRemoverRequest
    {
        public bool BypassCache { get; set; }
        public string? CacheKey { get; set; }
        public string[]? CacheGroupKey { get; set; }
    }
}
