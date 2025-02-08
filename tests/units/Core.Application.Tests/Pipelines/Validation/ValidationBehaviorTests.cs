using MediatR;
using Moq;
using NArchitecture.Core.Application.Pipelines.Validation;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Validation.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Validation;

public class ValidationBehaviorTests
{
    public class DummyRequest : IRequest<DummyResponse>
    {
        public string? Data { get; set; }
    }

    public class DummyResponse
    {
        public string? Result { get; set; }
    }

    /// <summary>
    /// Tests that a valid request proceeds through the pipeline and returns the expected response.
    /// </summary>
    [Fact(DisplayName = "Handle_GivenValidRequest_ShouldProceed")]
    public async Task Handle_GivenValidRequest_ShouldProceed()
    {
        // Arrange
        var validResult = new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>() };
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(validResult);

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var dummyRequest = new DummyRequest { Data = "Test" };
        var expectedResponse = new DummyResponse { Result = "OK" };

        // Act
        DummyResponse response = await behavior.Handle(
            dummyRequest,
            () => Task.FromResult(expectedResponse),
            CancellationToken.None
        );

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
        validatorMock.Verify(v => v.Validate(It.IsAny<DummyRequest>()), Times.Once);
    }

    /// <summary>
    /// Tests that an invalid request causes a ValidationException with appropriate error details.
    /// </summary>
    [Theory(DisplayName = "Handle_GivenInvalidRequest_ShouldThrowValidationException")]
    [InlineData("Name", "Required")]
    [InlineData("Age", "Must be positive")]
    public async Task Handle_GivenInvalidRequest_ShouldThrowValidationException(string propertyName, string errorMessage)
    {
        // Arrange
        var invalidResult = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { new ValidationError(propertyName, errorMessage) },
        };
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(invalidResult);

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var dummyRequest = new DummyRequest { Data = "Test Fail" };

        Task<DummyResponse> Next() => Task.FromResult(new DummyResponse());

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(dummyRequest, Next, CancellationToken.None)
        );

        // Assert
        exception.Errors.ShouldNotBeEmpty();
        var validationError = exception.Errors.FirstOrDefault();
        validationError.ShouldNotBeNull();
        validationError.Property.ShouldBe(propertyName);
        validationError.Errors!.ShouldContain(errorMessage);

        validatorMock.Verify(v => v.Validate(It.IsAny<DummyRequest>()), Times.Once);
    }

    /// <summary>
    /// Tests that behavior proceeds when no validator is registered.
    /// </summary>
    [Fact(DisplayName = "Handle_WithNoValidator_ShouldProceed")]
    public async Task Handle_WithNoValidator_ShouldProceed()
    {
        // Arrange
        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>();
        var request = new DummyRequest();
        var expectedResponse = new DummyResponse { Result = "OK" };

        // Act
        var response = await behavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
    }

    /// <summary>
    /// Tests that null validation results are handled gracefully.
    /// </summary>
    [Fact(DisplayName = "Handle_WithNullValidationResults_ShouldProceed")]
    public async Task Handle_WithNullValidationResults_ShouldProceed()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        validatorMock
            .Setup(v => v.Validate(It.IsAny<DummyRequest>()))
            .Returns(new ValidationResult { IsValid = true, Errors = null });

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var request = new DummyRequest();
        var expectedResponse = new DummyResponse { Result = "OK" };

        // Act
        var response = await behavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
    }

    /// <summary>
    /// Tests that when multiple validation errors occur, all errors are collected.
    /// </summary>
    [Fact(DisplayName = "Handle_WithMultipleValidationErrors_ShouldCollectAllErrors")]
    public async Task Handle_WithMultipleValidationErrors_ShouldCollectAllErrors()
    {
        // Arrange
        var error1 = new ValidationError("Property1", "Error1");
        var error2 = new ValidationError("Property2", "Error2");

        var validationResult = new ValidationResult { IsValid = false, Errors = new[] { error1, error2 } };

        var validatorMock = new Mock<IValidator<DummyRequest>>();
        validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(validationResult);

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var request = new DummyRequest();

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(request, () => Task.FromResult(new DummyResponse()), CancellationToken.None)
        );

        // Assert
        exception.Errors.Count().ShouldBe(2);
        exception.Errors.ShouldContain(e => e.Property == "Property1" && e.Errors!.Contains("Error1"));
        exception.Errors.ShouldContain(e => e.Property == "Property2" && e.Errors!.Contains("Error2"));
    }
}
