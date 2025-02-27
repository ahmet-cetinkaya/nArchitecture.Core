using System.Diagnostics;
using Moq;
using NArchitecture.Core.Application.Pipelines.Performance;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Performance;

[Trait("Category", "Performance")]
public class PerformanceBehaviorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Stopwatch _stopwatch;
    private readonly PerformanceBehavior<TestRequest, TestResponse> _behavior;

    public PerformanceBehaviorTests()
    {
        _loggerMock = new();
        _stopwatch = new();
        _behavior = new(_loggerMock.Object, _stopwatch);
    }

    [Fact(DisplayName = "Handle should log performance warning when request exceeds interval threshold")]
    public async Task Handle_WhenRequestExceedsInterval_ShouldLogPerformanceWarning()
    {
        // Arrange
        var request = new TestRequest { IntervalOptions = new(1) };
        var response = new TestResponse();
        string? loggedMessage = null;

        _ = _loggerMock
            .Setup(x => x.InformationAsync(It.IsAny<string>()))
            .Callback<string>(msg => loggedMessage = msg)
            .Returns(Task.CompletedTask);

        Task<TestResponse> next()
        {
            Thread.Sleep(1100); // Simulate work that takes longer than interval
            return Task.FromResult(response);
        }

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(response);
        _ = loggedMessage.ShouldNotBeNull();
        loggedMessage.ShouldContain("Performance ->");
        loggedMessage.ShouldContain("TestRequest");
        loggedMessage.ShouldContain("took");
        loggedMessage.ShouldContain("exceeding the threshold of 1s");
    }

    [Fact(DisplayName = "Handle should not log when request completes within interval threshold")]
    public async Task Handle_WhenRequestWithinInterval_ShouldNotLog()
    {
        // Arrange
        var request = new TestRequest { IntervalOptions = new(1) };
        var response = new TestResponse();

        Task<TestResponse> next() => Task.FromResult(response);

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(response);
        _loggerMock.Verify(x => x.InformationAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Handle should restart stopwatch when exception occurs")]
    public async Task Handle_WhenExceptionOccurs_ShouldRestartStopwatch()
    {
        // Arrange
        var request = new TestRequest { IntervalOptions = new(1) };
        var expectedException = new InvalidOperationException("Test exception");

        Task<TestResponse> next() => throw expectedException;

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behavior.Handle(request, next, CancellationToken.None)
        );

        _stopwatch.IsRunning.ShouldBeFalse("Stopwatch should be reset even after exception");
    }

    [Theory(DisplayName = "Handle should log appropriately with different interval thresholds")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Handle_WithDifferentIntervals_ShouldLogAppropriately(int intervalSeconds)
    {
        // Arrange
        var request = new TestRequest { IntervalOptions = new(intervalSeconds) };
        var response = new TestResponse();
        string? loggedMessage = null;

        _ = _loggerMock
            .Setup(x => x.InformationAsync(It.IsAny<string>()))
            .Callback<string>(msg => loggedMessage = msg)
            .Returns(Task.CompletedTask);

        Task<TestResponse> next()
        {
            Thread.Sleep(100); // Simulate some work
            return Task.FromResult(response);
        }

        // Act
        TestResponse result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(response);
        if (intervalSeconds == 0)
        {
            _ = loggedMessage.ShouldNotBeNull();
            loggedMessage.ShouldContain($"threshold of {intervalSeconds}s");
        }
    }

    [Fact(DisplayName = "Handle should time multiple requests independently")]
    public async Task Handle_MultipleRequests_ShouldTimeIndependently()
    {
        // Arrange
        var request = new TestRequest { IntervalOptions = new(1) };
        var response = new TestResponse();
        var loggedMessages = new List<string>();

        _ = _loggerMock
            .Setup(x => x.InformationAsync(It.IsAny<string>()))
            .Callback<string>(loggedMessages.Add)
            .Returns(Task.CompletedTask);

        Task<TestResponse> fastNext() => Task.FromResult(response);
        Task<TestResponse> slowNext()
        {
            Thread.Sleep(1200);
            return Task.FromResult(response);
        }

        // Act
        _ = await _behavior.Handle(request, fastNext, CancellationToken.None);
        _ = await _behavior.Handle(request, slowNext, CancellationToken.None);
        _ = await _behavior.Handle(request, fastNext, CancellationToken.None);

        // Assert
        loggedMessages.Count.ShouldBe(1, "Only the slow request should log");
        loggedMessages[0].ShouldContain("exceeding the threshold");
    }

    public class TestRequest : IRequest<TestResponse>, IIntervalRequest
    {
        public IntervalOptions IntervalOptions { get; init; }
    }

    public class TestResponse { }
}
