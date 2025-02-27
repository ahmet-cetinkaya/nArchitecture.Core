using System.Text.Json;
using Moq;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions.Models;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Logging;

[Trait("Category", "Logging")]
public class LoggingBehaviorTests
{
    private readonly Mock<ILogger> _loggerMock;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact(DisplayName = "Handle should log request details when request is valid")]
    public async Task Handle_ShouldLogRequestDetails_WhenRequestIsValid()
    {
        // Arrange: Create a test request and expected response.
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        // Act: Execute logging behavior.
        TestResponse response = await loggingBehavior.Handle(
            request,
            () => Task.FromResult(expectedResponse),
            CancellationToken.None
        );

        // Assert: Verify response and that request details are logged.
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(
            x => x.InformationAsync(It.Is<string>(s => s.Contains("testuser") && s.Contains(nameof(TestRequest)))),
            Times.Once
        );
    }

    [Fact(DisplayName = "Handle should use question mark as username when user identity is null")]
    public async Task Handle_ShouldUseQuestionMark_WhenUserIdentityIsNull()
    {
        // Arrange: Create a request with "?" as username.
        var request = new TestRequest("?") { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        // Act: Execute logging behavior.
        TestResponse response = await loggingBehavior.Handle(
            request,
            () => Task.FromResult(expectedResponse),
            CancellationToken.None
        );

        // Assert: Verify log contains "?".
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(s => s.Contains("?"))), Times.Once);
    }

    [Fact(DisplayName = "Handle should serialize log details correctly when request contains data")]
    public async Task Handle_ShouldSerializeLogDetailsCorrectly_WhenRequestContainsData()
    {
        // Arrange: Create a request with valid data.
        var request = new TestRequest("testuser") { Id = 42, Name = "Test Data" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert: Verify log details are serialized correctly.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        logDetail!.Parameters.Count.ShouldBe(1);
        logDetail.Parameters[0].Type.ShouldBe(nameof(TestRequest));
        logDetail.User.ShouldBe("testuser");
    }

    [Theory(DisplayName = "Handle should log correct username with different identities")]
    [InlineData("admin", "admin")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public async Task Handle_ShouldLogCorrectUsername_WithDifferentIdentities(string? username, string expectedUsername)
    {
        // Arrange: Create a request with varying username.
        var request = new TestRequest(username!) { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that the log contains the expected username.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        logDetail!.User.ShouldBe(expectedUsername);
    }

    [Fact(DisplayName = "Handle should log and propagate exception when next handler throws")]
    public async Task Handle_ShouldLogAndPropagateException_WhenNextHandlerThrows()
    {
        // Arrange: Create a request and configure next delegate to throw.
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        static Task<TestResponse> next() => throw new InvalidOperationException("Test exception");

        // Act & Assert: Verify exception is thrown and logging occurs.
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await loggingBehavior.Handle(request, next, CancellationToken.None)
        );

        _loggerMock.Verify(x => x.InformationAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "Handle should log correct method name")]
    public async Task Handle_ShouldLogCorrectMethodName()
    {
        // Arrange: Create a test request.
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(
            request,
            () => Task.FromResult(new TestResponse { Result = "Test" }),
            CancellationToken.None
        );

        // Assert: Verify that the log detail contains the method name.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        _ = logDetail!.MethodName.ShouldNotBeNull();
        logDetail.MethodName.ShouldContain("RequestHandlerDelegate");
    }

    [Fact(DisplayName = "Handle should exclude specified parameters when exclusions are provided")]
    public async Task Handle_ShouldExcludeSpecifiedParameters_WhenExcludeParametersProvided()
    {
        // Arrange: Create a request with parameters to be excluded.
        var request = new TestRequestWithExclusion
        {
            Id = 1,
            Name = "Test",
            Password = "secret",
        };
        var loggingBehavior = new LoggingBehavior<TestRequestWithExclusion, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that excluded parameters are not logged.
        _ = capturedLogMessage.ShouldNotBeNull();
        capturedLogMessage.ShouldNotContain("secret");
        capturedLogMessage.ShouldContain("Test"); // Name should still be included
    }

    [Theory(DisplayName = "Handle should mask specified parameters when masking is enabled")]
    [InlineData("1234567890", "12******90")]
    [InlineData("test@email.com", "test******l.com")]
    public async Task Handle_ShouldMaskSpecifiedParameters_WhenMaskingIsEnabled(string value, string expected)
    {
        // Arrange: Create a request where sensitive data should be masked.
        var request = new TestRequestWithMasking { SensitiveData = value };
        var loggingBehavior = new LoggingBehavior<TestRequestWithMasking, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that the sensitive data is masked as expected.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            logDetail!.Parameters[0].Value.ToString()!
        );
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    private class TestRequestWithMasking : IRequest<TestResponse>, ILoggableRequest
    {
        public TestRequestWithMasking() { }

        public string SensitiveData { get; set; } = string.Empty;

        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(name: nameof(SensitiveData), mask: true, keepStartChars: 2, keepEndChars: 3),
                ]
            );
    }

    private class TestRequestWithExclusion : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LogOptions LogOptions => new(excludeParameters: [nameof(Password)]);
    }

    [Fact(DisplayName = "Handle should log response when enabled")]
    public async Task Handle_ShouldLogResponse_WhenEnabled()
    {
        // Arrange
        var request = new TestRequestWithResponseLogging { Id = 1 };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequestWithResponseLogging, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        capturedLogMessage.ShouldContain("Success");
    }

    private class TestRequestWithResponseLogging : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public LogOptions LogOptions => new(logResponse: true);
    }

    private class TestRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        private readonly string _user;

        public TestRequest(string user = "testuser")
        {
            _user = user;
        }

        public LogOptions LogOptions => new(_user);
    }

    private class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    [Theory(DisplayName = "Handle should apply complex masking with different parameters")]
    [InlineData("password123", 2, 3, '*', "pa*****123")]
    [InlineData("short", 2, 2, '#', "sh#rt")]
    [InlineData("test", 4, 0, '*', "test")]
    [InlineData("x", 1, 1, '*', "x")]
    public async Task Handle_ShouldApplyComplexMasking_WithDifferentParameters(
        string value,
        int keepStart,
        int keepEnd,
        char maskChar,
        string expected
    )
    {
        // Arrange: Create a request with complex masking parameters.
        var request = new ComplexMaskingRequest
        {
            SensitiveData = value,
            KeepStartChars = keepStart,
            KeepEndChars = keepEnd,
            MaskChar = maskChar,
        };

        var loggingBehavior = new LoggingBehavior<ComplexMaskingRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that the sensitive data is masked as expected.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            logDetail!.Parameters[0].Value.ToString()!
        );
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    [Fact(DisplayName = "Handle should handle multiple exclusions and masks")]
    public async Task Handle_ShouldHandleMultipleExclusionsAndMasks()
    {
        // Arrange: Create a request with multiple exclusions and masks.
        var request = new MultipleExclusionRequest
        {
            PublicData = "visible",
            Password = "secret123",
            Email = "test@example.com",
            ApiKey = "ak_123456789",
        };

        var loggingBehavior = new LoggingBehavior<MultipleExclusionRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that excluded and masked parameters are handled correctly.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            logDetail!.Parameters[0].Value.ToString()!
        );

        parameters!["PublicData"].ToString().ShouldBe("visible");
        parameters!.ContainsKey("Password").ShouldBeFalse();
        parameters!["Email"].ToString().ShouldBe("test******com");
        parameters!["ApiKey"].ToString().ShouldBe("ak_***");
    }

    [Fact(DisplayName = "Handle should log complex response when enabled")]
    public async Task Handle_ShouldLogComplexResponse_WhenEnabled()
    {
        // Arrange: Create a request and complex response.
        var request = new ComplexResponseRequest();
        var complexResponse = new ComplexResponse
        {
            Id = 1,
            Name = "Test",
            Details = ["detail1", "detail2"],
            Timestamp = DateTime.UtcNow,
        };

        var loggingBehavior = new LoggingBehavior<ComplexResponseRequest, ComplexResponse>(_loggerMock.Object);

        var logMessages = new List<string>();
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(logMessages.Add);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(complexResponse), CancellationToken.None);

        // Assert: Verify that the complex response is logged correctly.
        logMessages.Count.ShouldBe(2); // Should have request and response logs
        logMessages[1].ShouldContain("detail1");
        logMessages[1].ShouldContain("detail2");
        logMessages[1].ShouldContain(complexResponse.Name);
    }

    private class ComplexMaskingRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string SensitiveData { get; set; } = string.Empty;
        public int KeepStartChars { get; set; }
        public int KeepEndChars { get; set; }
        public char MaskChar { get; set; }

        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        nameof(SensitiveData),
                        mask: true,
                        maskChar: MaskChar,
                        keepStartChars: KeepStartChars,
                        keepEndChars: KeepEndChars
                    ),
                ]
            );
    }

    private class MultipleExclusionRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string PublicData { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(nameof(Password)),
                    new LogExcludeParameter(name: nameof(Email), mask: true, keepStartChars: 4, keepEndChars: 3),
                    new LogExcludeParameter(name: nameof(ApiKey), mask: true, keepStartChars: 3, keepEndChars: 0),
                ]
            );
    }

    private class ComplexResponseRequest : IRequest<ComplexResponse>, ILoggableRequest
    {
        public LogOptions LogOptions => new(logResponse: true);
    }

    private class ComplexResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    [Fact(DisplayName = "Handle should apply default masking when applicable")]
    public async Task Handle_ShouldApplyDefaultMasking_WhenApplicable()
    {
        // Arrange: Create a request with default masking.
        var request = new TestDefaultMaskingRequest { MaskedData = "abcdefghi" };
        var loggingBehavior = new LoggingBehavior<TestDefaultMaskingRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act: Execute logging behavior.
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert: Verify that the default masking is applied correctly.
        _ = capturedLogMessage.ShouldNotBeNull();
        LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            logDetail!.Parameters[0].Value.ToString()!
        );
        parameters!["MaskedData"].ToString().ShouldBe("ab-----hi");
    }

    private class TestDefaultMaskingRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string MaskedData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(MaskedData),
                        mask: true,
                        maskChar: '-',
                        keepStartChars: 2,
                        keepEndChars: 2
                    ),
                ]
            );
    }
}
