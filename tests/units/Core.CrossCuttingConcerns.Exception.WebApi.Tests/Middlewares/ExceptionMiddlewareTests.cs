using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Middleware;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;
using Shouldly;
using NArchitecture.Core.Validation.Abstractions;

namespace Core.CrossCuttingConcerns.Exception.WebApi.Tests.Middlewares;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly DefaultHttpContext _httpContext;

    public ExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger>();
        _contextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _contextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    [Fact]
    public async Task Invoke_WhenNoException_ShouldCompleteSuccessfully()
    {
        // Arrange
        RequestDelegate next = async (context) => await Task.CompletedTask;
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(x => x.Information(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_WhenBusinessException_ShouldReturn400StatusCode()
    {
        // Arrange
        var exceptionMessage = "Business rule violated";
        RequestDelegate next = _ => throw new BusinessException(exceptionMessage);
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        _loggerMock.Verify(x => x.Information(It.Is<string>(s => s.IndexOf(exceptionMessage) >= 0)));
    }

    [Fact]
    public async Task Invoke_WhenValidationException_ShouldReturn400StatusCode()
    {
        // Arrange
        var validationErrors = new List<ValidationError>
        {
            new("Property", "Error message"),
        };
        RequestDelegate next = _ => throw new ValidationException(validationErrors);
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Invoke_WhenAuthorizationException_ShouldReturn401StatusCode()
    {
        // Arrange
        RequestDelegate next = _ => throw new AuthorizationException("Unauthorized access");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Invoke_WhenNotFoundException_ShouldReturn404StatusCode()
    {
        // Arrange
        RequestDelegate next = _ => throw new NotFoundException("Resource not found");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    private void SetupUserIdentity(string? userName)
    {
        if (string.IsNullOrEmpty(userName))
            return;

        var claims = new List<Claim> { new(ClaimTypes.Name, userName) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
    }

    private bool VerifyLogDetail(string? logJson, string expectedUser)
    {
        if (string.IsNullOrEmpty(logJson))
            return false;

        try
        {
            var logDetail = JsonSerializer.Deserialize<LogDetail>(logJson);
            return logDetail != null && (string.IsNullOrWhiteSpace(logDetail.User) ? "?" : logDetail.User) == expectedUser;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedUser_ShouldLogUserName()
    {
        // Arrange
        const string expectedUserName = "testUser";
        SetupUserIdentity(expectedUserName);
        RequestDelegate next = _ => throw new System.Exception("Test exception");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Information(It.Is<string>(s => VerifyLogDetail(s, expectedUserName))),
            Times.Once,
            "Log should contain the authenticated user's name"
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invoke_WithoutAuthenticatedUser_ShouldLogQuestionMark(string? userName)
    {
        // Arrange
        SetupUserIdentity(userName);
        RequestDelegate next = _ => throw new System.Exception("Test exception");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.Information(It.Is<string>(s => VerifyLogDetail(s, "?"))),
            Times.Once,
            "Log should contain '?' for unauthenticated or empty user name"
        );
    }
}
