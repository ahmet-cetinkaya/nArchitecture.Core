using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Handlers;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;
using NArchitecture.Core.Validation.Abstractions;
using Shouldly;

namespace Core.CrossCuttingConcerns.Exception.WebApi.Tests.Handlers;

/// <summary>
/// Tests for verifying HTTP exception handler responses.
/// </summary>
public class HttpExceptionHandlerTests
{
    private readonly HttpExceptionHandler _handler;
    private readonly DefaultHttpContext _httpContext;
    private readonly MemoryStream _bodyStream;

    public HttpExceptionHandlerTests()
    {
        _handler = new HttpExceptionHandler();
        _httpContext = new DefaultHttpContext();
        _bodyStream = new MemoryStream();
        _httpContext.Response.Body = _bodyStream;
        _handler.Response = _httpContext.Response;
    }

    /// <summary>
    /// Helper method to get response body from test context.
    /// </summary>
    private async Task<T> GetResponseBody<T>()
    {
        _bodyStream.Seek(0, SeekOrigin.Begin);
        string responseBody = await new StreamReader(_bodyStream).ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(responseBody)
            ?? throw new InvalidOperationException("Response body could not be deserialized");
    }

    /// <summary>
    /// Tests handling of business exceptions.
    /// </summary>
    [Fact(DisplayName = "HandleException should set 400 status code for BusinessException")]
    public async Task HandleException_WhenBusinessException_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        const string errorMessage = "Business rule violation";
        var exception = new BusinessException(errorMessage);

        // Act
        await _handler.HandleException(exception);
        var problemDetails = await GetResponseBody<BusinessProblemDetails>();

        // Assert
        _handler.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemDetails.Detail.ShouldBe(errorMessage);
    }

    /// <summary>
    /// Tests handling of validation exceptions with multiple errors.
    /// </summary>
    [Fact(DisplayName = "HandleException should handle ValidationException with multiple errors")]
    public async Task HandleException_WhenValidationException_ShouldReturn400WithAllErrors()
    {
        // Arrange
        var validationErrors = new List<ValidationError>
        {
            new("Email", "Invalid email format"),
            new("Age", new[] { "Must be positive", "Must be less than 150" }),
        };
        var exception = new ValidationException(validationErrors);

        // Act
        await _handler.HandleException(exception);
        var problemDetails = await GetResponseBody<ValidationProblemDetails>();

        // Assert
        _handler.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemDetails.Errors.Count().ShouldBe(2);
        problemDetails.Errors.ShouldContain(e => e.PropertyName == "Email");
        problemDetails.Errors.ShouldContain(e => e.PropertyName == "Age" && e.Errors!.Count() == 2);
    }

    /// <summary>
    /// Tests handling of authorization exceptions.
    /// </summary>
    [Fact(DisplayName = "HandleException should set 401 status code for AuthorizationException")]
    public async Task HandleException_WhenAuthorizationException_ShouldReturn401WithProblemDetails()
    {
        // Arrange
        const string errorMessage = "Unauthorized access";
        var exception = new AuthorizationException(errorMessage);

        // Act
        await _handler.HandleException(exception);
        var problemDetails = await GetResponseBody<AuthorizationProblemDetails>();

        // Assert
        _handler.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.ShouldBe(errorMessage);
    }

    /// <summary>
    /// Tests handling of not found exceptions.
    /// </summary>
    [Fact(DisplayName = "HandleException should set 404 status code for NotFoundException")]
    public async Task HandleException_WhenNotFoundException_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        const string errorMessage = "Resource not found";
        var exception = new NotFoundException(errorMessage);

        // Act
        await _handler.HandleException(exception);
        var problemDetails = await GetResponseBody<NotFoundProblemDetails>();

        // Assert
        _handler.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemDetails.Detail.ShouldBe(errorMessage);
    }

    /// <summary>
    /// Tests handling of generic exceptions with different messages.
    /// </summary>
    [Theory(DisplayName = "HandleException should handle generic exceptions with different messages")]
    [InlineData("Database connection failed")]
    [InlineData("System is unavailable")]
    [InlineData("")]
    public async Task HandleException_WhenGenericException_ShouldReturn500WithProblemDetails(string errorMessage)
    {
        // Arrange
        var exception = new System.Exception(errorMessage);
        var expectedMessage = string.IsNullOrWhiteSpace(errorMessage) ? "An internal server error occurred" : errorMessage;

        // Act
        await _handler.HandleException(exception);
        var problemDetails = await GetResponseBody<InternalServerErrorProblemDetails>();

        // Assert
        _handler.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.ShouldBe(expectedMessage);
    }

    /// <summary>
    /// Tests response property behavior when not initialized.
    /// </summary>
    [Fact(DisplayName = "Response property should throw when not initialized")]
    public void Response_WhenNotSet_ShouldThrowNullReferenceException()
    {
        // Arrange
        var handler = new HttpExceptionHandler();

        // Act & Assert
        Should.Throw<NullReferenceException>(() => handler.Response);
    }
}
