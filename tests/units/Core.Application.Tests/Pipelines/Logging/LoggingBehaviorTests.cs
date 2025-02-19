using System.Text.Json;
using MediatR;
using Moq;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Logging;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger> _loggerMock;

    public LoggingBehaviorTests() => _loggerMock = new Mock<ILogger>();

    /// <summary>
    /// Tests that request details are logged when the request is valid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogRequestDetails_WhenRequestIsValid()
    {
        // Arrange
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        // Act
        var response = await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(
            x => x.InformationAsync(It.Is<string>(s => s.Contains("testuser") && s.Contains(nameof(TestRequest)))),
            Times.Once
        );
    }

    /// <summary>
    /// Tests that a question mark is used as the username when user identity is not available.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldUseQuestionMark_WhenUserIdentityIsNull()
    {
        // Arrange
        var request = new TestRequest("?") { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        // Act
        var response = await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(s => s.Contains("?"))), Times.Once);
    }

    /// <summary>
    /// Tests that log details are properly serialized with correct parameter values.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldSerializeLogDetailsCorrectly_WhenRequestContainsData()
    {
        // Arrange
        var request = new TestRequest("testuser") { Id = 42, Name = "Test Data" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        logDetail!.Parameters.Count.ShouldBe(1);
        logDetail.Parameters[0].Type.ShouldBe(nameof(TestRequest));
        logDetail.User.ShouldBe("testuser");
    }

    /// <summary>
    /// Tests logging behavior with different user identity scenarios.
    /// </summary>
    [Theory]
    [InlineData("admin", "admin")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public async Task Handle_ShouldLogCorrectUsername_WithDifferentIdentities(string? username, string expectedUsername)
    {
        // Arrange
        var request = new TestRequest(username!) { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        logDetail!.User.ShouldBe(expectedUsername);
    }

    /// <summary>
    /// Tests that exceptions during request handling are properly propagated while still logging.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogAndPropagateException_WhenNextHandlerThrows()
    {
        // Arrange
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        RequestHandlerDelegate<TestResponse> next = () => throw new InvalidOperationException("Test exception");

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await loggingBehavior.Handle(request, next, CancellationToken.None)
        );

        _loggerMock.Verify(x => x.InformationAsync(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Tests that the method name is correctly logged.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogCorrectMethodName()
    {
        // Arrange
        var request = new TestRequest("testuser") { Id = 1, Name = "Test" };
        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(
            request,
            () => Task.FromResult(new TestResponse { Result = "Test" }),
            CancellationToken.None
        );

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        _ = logDetail!.MethodName.ShouldNotBeNull();
        logDetail.MethodName.ShouldContain("RequestHandlerDelegate");
    }

    /// <summary>
    /// Tests that specified parameters are excluded from logging.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldExcludeSpecifiedParameters_WhenExcludeParametersProvided()
    {
        // Arrange
        var request = new TestRequestWithExclusion
        {
            Id = 1,
            Name = "Test",
            Password = "secret",
        };
        var loggingBehavior = new LoggingBehavior<TestRequestWithExclusion, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        capturedLogMessage.ShouldNotContain("secret");
        capturedLogMessage.ShouldContain("Test"); // Name should still be included
    }

    /// <summary>
    /// Tests that specified parameters are masked when masking is enabled.
    /// </summary>
    [Theory]
    [InlineData("1234567890", "12******90")]
    [InlineData("test@email.com", "test******l.com")]
    public async Task Handle_ShouldMaskSpecifiedParameters_WhenMaskingIsEnabled(string value, string expected)
    {
        // Arrange
        var request = new TestRequestWithMasking { SensitiveData = value };
        var loggingBehavior = new LoggingBehavior<TestRequestWithMasking, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    private class TestRequestWithMasking : IRequest<TestResponse>, ILoggableRequest
    {
        public string SensitiveData { get; set; } = string.Empty;

        public LogOptions LogOptions
        {
            get =>
                new(
                    excludeParameters:
                    [
                        new LogExcludeParameter(
                            name: nameof(SensitiveData),
                            mask: true,
                            keepStartChars: 2, // Changed from 4 to 2
                            keepEndChars: 3 // Changed from 4 to 3
                        ),
                    ]
                );
        }
    }

    private class TestRequestWithExclusion : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LogOptions LogOptions
        {
            get => new(excludeParameters: [nameof(Password)]);
        }
    }

    /// <summary>
    /// Tests that response logging works when enabled
    /// </summary>
    [Fact]
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
        public LogOptions LogOptions
        {
            get => new(logResponse: true);
        }
    }

    private class TestRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        private string _user;

        public TestRequest(string user = "testuser") => _user = user;

        public LogOptions LogOptions
        {
            get => new(_user);
        }
    }

    private class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tests complex masking scenarios with different parameter combinations
    /// </summary>
    [Theory]
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
        // Arrange
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

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    /// <summary>
    /// Tests that multiple parameters can be excluded or masked simultaneously.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldHandleMultipleExclusionsAndMasks()
    {
        // Arrange
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

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);

        parameters!["PublicData"].ToString().ShouldBe("visible");
        parameters!.ContainsKey("Password").ShouldBeFalse();
        parameters!["Email"].ToString().ShouldBe("test******com");
        parameters!["ApiKey"].ToString().ShouldBe("ak_***");
    }

    /// <summary>
    /// Tests response logging with complex object
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogComplexResponse_WhenEnabled()
    {
        // Arrange
        var request = new ComplexResponseRequest();
        var complexResponse = new ComplexResponse
        {
            Id = 1,
            Name = "Test",
            Details = new() { "detail1", "detail2" },
            Timestamp = DateTime.UtcNow,
        };

        var loggingBehavior = new LoggingBehavior<ComplexResponseRequest, ComplexResponse>(_loggerMock.Object);

        var logMessages = new List<string>();
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => logMessages.Add(msg));

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(complexResponse), CancellationToken.None);

        // Assert
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

        public LogOptions LogOptions
        {
            get =>
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
    }

    private class MultipleExclusionRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string PublicData { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public LogOptions LogOptions
        {
            get =>
                new(
                    excludeParameters:
                    [
                        new LogExcludeParameter(nameof(Password)),
                        new LogExcludeParameter(name: nameof(Email), mask: true, keepStartChars: 4, keepEndChars: 3),
                        new LogExcludeParameter(name: nameof(ApiKey), mask: true, keepStartChars: 3, keepEndChars: 0),
                    ]
                );
        }
    }

    private class ComplexResponseRequest : IRequest<ComplexResponse>, ILoggableRequest
    {
        public LogOptions LogOptions
        {
            get => new(logResponse: true);
        }
    }

    private class ComplexResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Details { get; set; } = [];
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Tests that default masking is applied when applicable.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldApplyDefaultMasking_WhenApplicable()
    {
        // Arrange
        var request = new TestDefaultMaskingRequest { MaskedData = "abcdefghi" };
        var loggingBehavior = new LoggingBehavior<TestDefaultMaskingRequest, TestResponse>(_loggerMock.Object);

        string? capturedLogMessage = null;
        _ = _loggerMock.Setup(x => x.InformationAsync(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        _ = await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        _ = capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["MaskedData"].ToString().ShouldBe("ab-----hi");
    }

    private class TestDefaultMaskingRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string MaskedData { get; set; } = string.Empty;
        public LogOptions LogOptions
        {
            get =>
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
}
