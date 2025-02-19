using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LoggingBehaviorBenchmarks
{
    private const int REQUEST_COUNT = 100;
    private TestRequest[] _multipleRequests = null!;
    private LoggingBehavior<TestRequest, TestResponse> _loggingBehavior = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private TestRequest _request = null!;
    private Consumer _consumer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _loggingBehavior = new(_loggerMock.Object);
        _request = new TestRequest
        {
            Email = "test@example.com",
            Password = "password123",
            SensitiveData = "sensitive@data.com",
        };
        _consumer = new Consumer();

        _multipleRequests = Enumerable
            .Range(0, REQUEST_COUNT)
            .Select(i => new TestRequest
            {
                Email = $"test{i}@example.com",
                Password = $"password{i}",
                SensitiveData = $"sensitive{i}@data.com",
            })
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task Handle_BasicRequest()
    {
        _ = await _loggingBehavior.Handle(
            _request,
            () => Task.FromResult(new TestResponse { Success = true }),
            CancellationToken.None
        );
    }

    [Benchmark]
    public async Task Handle_WithLargePayload()
    {
        var largeRequest = new TestRequest
        {
            Email = "test@example.com",
            Password = "password123",
            SensitiveData = string.Join("", Enumerable.Repeat("large-sensitive-data", 1000)),
        };

        _ = await _loggingBehavior.Handle(
            largeRequest,
            () => Task.FromResult(new TestResponse { Success = true }),
            CancellationToken.None
        );
    }

    [Benchmark]
    public async Task Handle_WithAsyncResponse()
    {
        _ = await _loggingBehavior.Handle(
            _request,
            async () =>
            {
                await Task.Delay(10); // Simulate async work
                return new TestResponse { Success = true };
            },
            CancellationToken.None
        );
    }

    [Benchmark]
    public async Task Handle_MultipleSequential()
    {
        foreach (var request in _multipleRequests)
        {
            _ = await _loggingBehavior.Handle(
                request,
                () => Task.FromResult(new TestResponse { Success = true }),
                CancellationToken.None
            );
        }
    }

    [Benchmark]
    public async Task Handle_MultipleParallel()
    {
        var tasks = _multipleRequests.Select(request =>
            _loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse { Success = true }), CancellationToken.None)
        );

        _ = await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Handle_MultipleBatched()
    {
        const int batchSize = 10;
        var batches = _multipleRequests
            .Select((request, index) => new { request, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.request));

        foreach (var batch in batches)
        {
            var tasks = batch.Select(request =>
                _loggingBehavior.Handle(
                    request,
                    () => Task.FromResult(new TestResponse { Success = true }),
                    CancellationToken.None
                )
            );

            _ = await Task.WhenAll(tasks);
        }
    }

    [Benchmark]
    public async Task Handle_ConcurrentWithThrottling()
    {
        using var semaphore = new SemaphoreSlim(5); // Max 5 concurrent requests
        var tasks = new List<Task>();

        foreach (var request in _multipleRequests)
        {
            await semaphore.WaitAsync();
            tasks.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        _ = await _loggingBehavior.Handle(
                            request,
                            () => Task.FromResult(new TestResponse { Success = true }),
                            CancellationToken.None
                        );
                    }
                    finally
                    {
                        _ = semaphore.Release();
                    }
                })
            );
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task Handle_ProducerConsumerPattern()
    {
        var channel = Channel.CreateBounded<TestRequest>(
            new BoundedChannelOptions(20) { FullMode = BoundedChannelFullMode.Wait }
        );

        var producer = Task.Run(async () =>
        {
            foreach (var request in _multipleRequests)
            {
                await channel.Writer.WriteAsync(request);
            }
            channel.Writer.Complete();
        });

        var consumers = Enumerable
            .Range(0, 3)
            .Select(_ =>
                Task.Run(async () =>
                {
                    await foreach (var request in channel.Reader.ReadAllAsync())
                    {
                        _ = await _loggingBehavior.Handle(
                            request,
                            () => Task.FromResult(new TestResponse { Success = true }),
                            CancellationToken.None
                        );
                    }
                })
            );

        await Task.WhenAll(consumers.Prepend(producer));
    }
}

internal class TestRequest : IRequest<TestResponse>, ILoggableRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SensitiveData { get; set; } = string.Empty;

    public LogOptions LogOptions =>
        new(
            user: "benchmarkuser",
            logResponse: true,
            excludeParameters:
            [
                new("Password", true, '*', 2, 2),
                new("SensitiveData", true, '*', 4, 5),
                new("Email", true, '*', 2, 3),
            ]
        );
}

public class TestResponse
{
    public bool Success { get; set; }

    public static implicit operator int(TestResponse v)
    {
        switch (v.Success)
        {
            case true:
                return 1;
            case false:
                return 0;
        }
    }
}
