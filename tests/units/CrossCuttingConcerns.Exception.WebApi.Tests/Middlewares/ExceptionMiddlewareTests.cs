using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Middleware;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions.Models;
using NArchitecture.Core.Validation.Abstractions;
using Shouldly;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Tests.Middlewares;

[Trait("Category", "Exception")]
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
        _ = _contextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    [Fact(DisplayName = "Invoke should complete successfully when no exception occurs")]
    public async Task Invoke_WhenNoException_ShouldCompleteSuccessfully()
    {
        // Arrange
        static async Task next(HttpContext context) => await Task.CompletedTask;
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(x => x.InformationAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Invoke should return 400 status code for business exceptions")]
    public async Task Invoke_WhenBusinessException_ShouldReturn400StatusCode()
    {
        // Arrange
        string exceptionMessage = "Business rule violated";
        Task next(HttpContext _) => throw new BusinessException(exceptionMessage);
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        _loggerMock.Verify(x => x.InformationAsync(It.Is<string>(s => s.IndexOf(exceptionMessage) >= 0)));
    }

    [Fact(DisplayName = "Invoke should return 400 status code for validation exceptions")]
    public async Task Invoke_WhenValidationException_ShouldReturn400StatusCode()
    {
        // Arrange
        var validationErrors = new List<ValidationError> { new("Property", new List<string> { "Error message" }) };
        Task next(HttpContext _) => throw new ValidationException(validationErrors);
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact(DisplayName = "Invoke should return 401 status code for authorization exceptions")]
    public async Task Invoke_WhenAuthorizationException_ShouldReturn401StatusCode()
    {
        // Arrange
        static Task next(HttpContext _) => throw new AuthorizationException("Unauthorized access");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact(DisplayName = "Invoke should return 404 status code for not found exceptions")]
    public async Task Invoke_WhenNotFoundException_ShouldReturn404StatusCode()
    {
        // Arrange
        static Task next(HttpContext _) => throw new NotFoundException("Resource not found");
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
            LogDetail? logDetail = JsonSerializer.Deserialize<LogDetail>(logJson);
            return logDetail != null && (string.IsNullOrWhiteSpace(logDetail.User) ? "?" : logDetail.User) == expectedUser;
        }
        catch
        {
            return false;
        }
    }

    [Fact(DisplayName = "Invoke should log authenticated user name when exception occurs")]
    public async Task Invoke_WithAuthenticatedUser_ShouldLogUserName()
    {
        // Arrange
        const string expectedUserName = "testUser";
        SetupUserIdentity(expectedUserName);
        static Task next(HttpContext _) => throw new System.Exception("Test exception");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.InformationAsync(It.Is<string>(s => VerifyLogDetail(s, expectedUserName))),
            Times.Once,
            "Log should contain the authenticated user's name"
        );
    }

    [Theory(DisplayName = "Invoke should log question mark for unauthenticated or empty user name")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invoke_WithoutAuthenticatedUser_ShouldLogQuestionMark(string? userName)
    {
        // Arrange
        SetupUserIdentity(userName);
        static Task next(HttpContext _) => throw new System.Exception("Test exception");
        var middleware = new ExceptionMiddleware(next, _contextAccessorMock.Object, _loggerMock.Object);

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        _loggerMock.Verify(
            x => x.InformationAsync(It.Is<string>(s => VerifyLogDetail(s, "?"))),
            Times.Once,
            "Log should contain '?' for unauthenticated or empty user name"
        );
    }
}
