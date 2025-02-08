using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;
using Shouldly;

namespace Core.Application.Tests.Pipelines.Logging;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    /// <summary>
    /// Tests that request details are properly logged when the request and user context are valid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogRequestDetails_WhenRequestIsValid()
    {
        // Arrange
        var request = new TestRequest { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        var httpContext = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test"));
        httpContext.User = user;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await loggingBehavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(
            x => x.Information(It.Is<string>(s => s.Contains("testuser") && s.Contains(nameof(TestRequest)))),
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
        var request = new TestRequest { Id = 1, Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await loggingBehavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.ShouldBe(expectedResponse);
        _loggerMock.Verify(x => x.Information(It.Is<string>(s => s.Contains("?"))), Times.Once);
    }

    /// <summary>
    /// Tests that the appropriate exception is thrown when HttpContext is null.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowException_WhenHttpContextIsNull()
    {
        // Arrange
        var request = new TestRequest { Id = 1, Name = "Test" };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        // Act & Assert
        await Should.ThrowAsync<NullReferenceException>(
            async () => await loggingBehavior.Handle(request, next, CancellationToken.None)
        );
    }

    /// <summary>
    /// Tests that log details are properly serialized with correct parameter values.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldSerializeLogDetailsCorrectly_WhenRequestContainsData()
    {
        // Arrange
        var request = new TestRequest { Id = 42, Name = "Test Data" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }));
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
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
        var request = new TestRequest { Id = 1, Name = "Test" };
        var httpContext = new DefaultHttpContext();
        if (username != null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }));
        }
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
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
        var request = new TestRequest { Id = 1, Name = "Test" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        RequestHandlerDelegate<TestResponse> next = () => throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await loggingBehavior.Handle(request, next, CancellationToken.None)
        );

        _loggerMock.Verify(x => x.Information(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Tests that the method name is correctly logged.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogCorrectMethodName()
    {
        // Arrange
        var request = new TestRequest { Id = 1, Name = "Test" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(
            request,
            () => Task.FromResult(new TestResponse { Result = "Test" }),
            CancellationToken.None
        );

        // Assert
        capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        logDetail!.MethodName.ShouldNotBeNull();
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
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequestWithExclusion, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
        capturedLogMessage.ShouldNotContain("secret");
        capturedLogMessage.ShouldContain("Test"); // Name should still be included
    }

    [Theory]
    [InlineData("1234567890", "12******90")]
    [InlineData("test@email.com", "test******l.com")]
    public async Task Handle_ShouldMaskSpecifiedParameters_WhenMaskingIsEnabled(string value, string expected)
    {
        // Arrange
        var request = new TestRequestWithMasking { SensitiveData = value };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequestWithMasking, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    private class TestRequestWithMasking : IRequest<TestResponse>, ILoggableRequest
    {
        public string SensitiveData { get; set; } = string.Empty;

        public LogOptions LogOptions =>
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

    private class TestRequestWithExclusion : IRequest<TestResponse>, ILoggableRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LogOptions LogOptions => new(excludeParameters: [nameof(Password)]);
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
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<TestRequestWithResponseLogging, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
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

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<ComplexMaskingRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["SensitiveData"].ToString().ShouldBe(expected);
    }

    /// <summary>
    /// Tests that multiple parameters can be excluded or masked simultaneously
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

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<MultipleExclusionRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
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
            Timestamp = DateTime.Now,
        };

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var loggingBehavior = new LoggingBehavior<ComplexResponseRequest, ComplexResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        var logMessages = new List<string>();
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => logMessages.Add(msg));

        // Act
        await loggingBehavior.Handle(request, () => Task.FromResult(complexResponse), CancellationToken.None);

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
        public List<string> Details { get; set; } = [];
        public DateTime Timestamp { get; set; }
    }

    [Fact]
    public async Task Handle_ShouldApplyDefaultMasking_WhenApplicable()
    {
        // Arrange
        var request = new TestDefaultMaskingRequest { MaskedData = "abcdefghi" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var behavior = new LoggingBehavior<TestDefaultMaskingRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );

        string? capturedLogMessage = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(msg => capturedLogMessage = msg);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        capturedLogMessage.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(capturedLogMessage);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["MaskedData"].ToString().ShouldBe("ab-----hi");
    }

    // New test request for default masking
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

    // Tests that an empty string is returned unchanged (line ~29)
    [Fact]
    public async Task Handle_ShouldReturnEmptyForEmptyValue()
    {
        var request = new TestEmptyValueRequest { Data = "" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestEmptyValueRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["Data"].ToString().ShouldBe("");
    }

    // Tests sensitive email branch short-circuit (line ~38): if value.Length < 9, returns original
    [Fact]
    public async Task Handle_ShouldReturnOriginalForShortSensitiveEmail()
    {
        var request = new TestSensitiveEmailShortRequest { Email = "a@b.cd" }; // length < 9
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestSensitiveEmailShortRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        // Expect the original value since branch returns immediately.
        parameters!["Email"].ToString().ShouldBe("a@b.cd");
    }

    // Tests general email branch short-circuit (line ~53) when value.Length < (KeepStart+KeepEnd)
    [Fact]
    public async Task Handle_ShouldReturnOriginalForShortGeneralEmail()
    {
        var request = new TestGeneralEmailShortRequest { ContactEmail = "ab@c.de" }; // length less than keepStart+keepEnd (e.g. 5+5=10)
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestGeneralEmailShortRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["ContactEmail"].ToString().ShouldBe("ab@c.de");
    }

    // Tests numeric branch short-circuit (line ~64) when length <= 4
    [Fact]
    public async Task Handle_ShouldReturnOriginalForShortNumericString()
    {
        var request = new TestNumericRequest { NumericData = "1234" }; // length == 4
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestNumericRequest, TestResponse>(_loggerMock.Object, _httpContextAccessorMock.Object);
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["NumericData"].ToString().ShouldBe("1234");
    }

    // Tests password branch short-circuit (line ~102) when value length <= (keepStart+keepEnd)
    [Fact]
    public async Task Handle_ShouldReturnOriginalForShortPassword()
    {
        var request = new TestPasswordShortRequest { Password = "password123" }; // length 11; use keepStart=6, keepEnd=6 so 6+6=12 > 11
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestPasswordShortRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        // Since condition is met, the original value is returned.
        parameters!["Password"].ToString().ShouldBe("password123");
    }

    // Tests default branch when KeepEndChars != 0 (already partly covered via TestDefaultMasking)
    // Also tests new branch when KeepEndChars == 0 is hit – but one such case is in MultipleExclusionRequest.
    // For completeness, add a test for non-email, non-digit, non-password default masking.
    [Fact]
    public async Task Handle_ShouldApplyDefaultMasking_ForNonSpecialValue()
    {
        var request = new TestOtherDataRequest { OtherData = "abcdefgh" }; // expect "ab!!!!gh" for keepStart 2, keepEnd 2, mask char '!'
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestOtherDataRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["OtherData"].ToString().ShouldBe("ab!!!!gh");
    }

    // New test request classes to trigger specific branches:

    private class TestEmptyValueRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string Data { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(name: nameof(Data), mask: true, maskChar: '*', keepStartChars: 2, keepEndChars: 2),
                ]
            );
    }

    private class TestSensitiveEmailShortRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string Email { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(Email),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 4, // unused in sensitive branch
                        keepEndChars: 5 // unused in sensitive branch
                    ),
                ]
            );
    }

    private class TestGeneralEmailShortRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string ContactEmail { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(ContactEmail),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 5,
                        keepEndChars: 5
                    ),
                ]
            );
    }

    private class TestNumericRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string NumericData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(NumericData),
                        mask: true,
                        maskChar: '#',
                        keepStartChars: 2, // irrelevant when length <=4
                        keepEndChars: 2
                    ),
                ]
            );
    }

    private class TestPasswordShortRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string Password { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(Password),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 6,
                        keepEndChars: 6
                    ),
                ]
            );
    }

    private class TestOtherDataRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string OtherData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(OtherData),
                        mask: true,
                        maskChar: '!',
                        keepStartChars: 2,
                        keepEndChars: 2
                    ),
                ]
            );
    }

    // New test: Cover sensitive email branch (line ~38) for property "SensitiveData"
    [Fact]
    public async Task Handle_ShouldMaskSensitiveEmailProperly()
    {
        // Arrange
        var request = new TestSensitiveEmailRequest { SensitiveData = "test@mail.com" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestSensitiveEmailRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        logMsg.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        // Expected: first 4 chars ("test") + fixed mask of 6 + last 5 ("l.com")
        parameters!["SensitiveData"].ToString().ShouldBe("test******l.com");
    }

    // New test: Cover password special branch (line ~102) where value == "password123"
    [Fact]
    public async Task Handle_ShouldMaskPasswordSpecialCase()
    {
        // Arrange: set keepStart=2, keepEnd=2 so that 11 > (2+2) and masking occurs.
        var request = new TestPasswordMaskingRequest { Password = "password123" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestPasswordMaskingRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        logMsg.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        // Expected: first 2 ("pa") + mask of 6 + last 2 ("23")
        parameters!["Password"].ToString().ShouldBe("pa******23");
    }

    // New test: Cover default branch when KeepEndChars is zero (lines ~121-128) for non-email, non-digit values.
    [Fact]
    public async Task Handle_ShouldApplyCustomMasking_WithKeepEndZero()
    {
        // Arrange: a non-email, non-digit, non-password string.
        var request = new TestCustomKeepEndZeroRequest { CustomData = "abcdefghij" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestCustomKeepEndZeroRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        logMsg.ShouldNotBeNull();
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        // Expected: since KeepEndChars == 0, return first 3 characters + fixed mask of 3.
        parameters!["CustomData"].ToString().ShouldBe("abc$$$");
    }

    // New test request classes:

    private class TestSensitiveEmailRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string SensitiveData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(SensitiveData),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 4,
                        keepEndChars: 5 // will not be used for SensitiveData branch
                    ),
                ]
            );
    }

    private class TestPasswordMaskingRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string Password { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(Password),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 2,
                        keepEndChars: 2
                    ),
                ]
            );
    }

    private class TestCustomKeepEndZeroRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string CustomData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(CustomData),
                        mask: true,
                        maskChar: '$',
                        keepStartChars: 3,
                        keepEndChars: 0
                    ),
                ]
            );
    }

    // Test line ~38: Test SensitiveData email masking
    [Fact]
    public async Task Handle_ShouldCorrectlyMaskSensitiveDataEmail()
    {
        // Arrange
        var request = new TestSensitiveDataEmailRequest { SensitiveData = "user@example.com" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestSensitiveDataEmailRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["SensitiveData"].ToString().ShouldBe("user******e.com");
    }

    // Test line ~102: Test special password123 case
    [Fact]
    public async Task Handle_ShouldCorrectlyMaskPassword123()
    {
        // Arrange
        var request = new TestPassword123Request { Password = "password123" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestPassword123Request, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["Password"].ToString().ShouldBe("pa*****123"); // 2 start, 3 end, length-5 mask chars
    }

    // Test lines ~121-128: Test KeepEndChars=0 case
    [Fact]
    public async Task Handle_ShouldApplyFixedMaskingWhenKeepEndCharsIsZero()
    {
        // Arrange
        var request = new TestZeroEndCharsRequest { Data = "TestingData" };
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var behavior = new LoggingBehavior<TestZeroEndCharsRequest, TestResponse>(
            _loggerMock.Object,
            _httpContextAccessorMock.Object
        );
        string? logMsg = null;
        _loggerMock.Setup(x => x.Information(It.IsAny<string>())).Callback<string>(m => logMsg = m);

        // Act
        await behavior.Handle(request, () => Task.FromResult(new TestResponse()), CancellationToken.None);

        // Assert
        var logDetail = JsonSerializer.Deserialize<LogDetail>(logMsg!);
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(logDetail!.Parameters[0].Value.ToString()!);
        parameters!["Data"].ToString().ShouldBe("Test***"); // First 4 chars + exactly 3 mask chars
    }

    // Required test classes
    private class TestSensitiveDataEmailRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string SensitiveData { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(SensitiveData),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 4,
                        keepEndChars: 5
                    ),
                ]
            );
    }

    private class TestPassword123Request : IRequest<TestResponse>, ILoggableRequest
    {
        public string Password { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(
                        name: nameof(Password),
                        mask: true,
                        maskChar: '*',
                        keepStartChars: 2,
                        keepEndChars: 3
                    ),
                ]
            );
    }

    private class TestZeroEndCharsRequest : IRequest<TestResponse>, ILoggableRequest
    {
        public string Data { get; set; } = string.Empty;
        public LogOptions LogOptions =>
            new(
                excludeParameters:
                [
                    new LogExcludeParameter(name: nameof(Data), mask: true, maskChar: '*', keepStartChars: 4, keepEndChars: 0),
                ]
            );
    }
}
