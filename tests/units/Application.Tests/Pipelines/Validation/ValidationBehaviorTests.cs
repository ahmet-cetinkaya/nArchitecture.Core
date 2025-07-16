using Moq;
using NArchitecture.Core.Application.Pipelines.Validation;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator.Abstractions;
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

    [Fact(DisplayName = "Handle_GivenValidRequest_ShouldProceed")]
    public async Task Handle_GivenValidRequest_ShouldProceed()
    {
        // Arrange
        var validResult = new ValidationResult { IsValid = true, Errors = Array.Empty<ValidationError>() };
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        _ = validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(validResult);

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
        _ = response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
        validatorMock.Verify(v => v.Validate(It.IsAny<DummyRequest>()), Times.Once);
    }

    [Theory(DisplayName = "Handle_GivenInvalidRequest_ShouldThrowValidationException")]
    [InlineData("Name", "Required")]
    [InlineData("Age", "Must be positive")]
    public async Task Handle_GivenInvalidRequest_ShouldThrowValidationException(string propertyName, string errorMessage)
    {
        // Arrange
        var invalidResult = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { new ValidationError(propertyName, new[] { errorMessage }) },
        };
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        _ = validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(invalidResult);

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var dummyRequest = new DummyRequest { Data = "Test Fail" };

        static Task<DummyResponse> Next() => Task.FromResult(new DummyResponse());

        // Act & Assert
        ValidationException exception = await Should.ThrowAsync<ValidationException>(() =>
            behavior.Handle(dummyRequest, Next, CancellationToken.None)
        );

        // Assert
        exception.Errors.ShouldNotBeEmpty();
        ValidationError validationError = exception.Errors.FirstOrDefault();
        validationError.ShouldNotBe(default);
        validationError.PropertyName.ShouldBe(propertyName);
        validationError.Errors!.ShouldContain(errorMessage);

        validatorMock.Verify(v => v.Validate(It.IsAny<DummyRequest>()), Times.Once);
    }

    [Fact(DisplayName = "Handle_WithNoValidator_ShouldProceed")]
    public async Task Handle_WithNoValidator_ShouldProceed()
    {
        // Arrange
        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>();
        var request = new DummyRequest();
        var expectedResponse = new DummyResponse { Result = "OK" };

        // Act
        DummyResponse response = await behavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        _ = response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
    }

    [Fact(DisplayName = "Handle_WithNullValidationResults_ShouldProceed")]
    public async Task Handle_WithNullValidationResults_ShouldProceed()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<DummyRequest>>();
        _ = validatorMock
            .Setup(v => v.Validate(It.IsAny<DummyRequest>()))
            .Returns(new ValidationResult { IsValid = true, Errors = null });

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var request = new DummyRequest();
        var expectedResponse = new DummyResponse { Result = "OK" };

        // Act
        DummyResponse response = await behavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        _ = response.ShouldNotBeNull();
        response.Result.ShouldBe("OK");
    }

    [Fact(DisplayName = "Handle_WithMultipleValidationErrors_ShouldCollectAllErrors")]
    public async Task Handle_WithMultipleValidationErrors_ShouldCollectAllErrors()
    {
        // Arrange
        var error1 = new ValidationError("Property1", new[] { "Error1" });
        var error2 = new ValidationError("Property2", new[] { "Error2" });

        var validationResult = new ValidationResult { IsValid = false, Errors = new[] { error1, error2 } };

        var validatorMock = new Mock<IValidator<DummyRequest>>();
        _ = validatorMock.Setup(v => v.Validate(It.IsAny<DummyRequest>())).Returns(validationResult);

        var behavior = new ValidationBehavior<DummyRequest, DummyResponse>(validatorMock.Object);
        var request = new DummyRequest();

        // Act & Assert
        ValidationException exception = await Should.ThrowAsync<ValidationException>(() =>
            behavior.Handle(request, () => Task.FromResult(new DummyResponse()), CancellationToken.None)
        );

        // Assert
        exception.Errors.Count().ShouldBe(2);
        exception.Errors.ShouldContain(e => e.PropertyName == "Property1" && e.Errors!.Contains("Error1"));
        exception.Errors.ShouldContain(e => e.PropertyName == "Property2" && e.Errors!.Contains("Error2"));
    }
}
