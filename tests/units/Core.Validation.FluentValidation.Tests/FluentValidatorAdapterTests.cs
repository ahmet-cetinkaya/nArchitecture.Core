using FluentValidation;
using FluentValidation.Results;
using Moq;
using Shouldly;
using FluentIValidator = FluentValidation.IValidator<NArchitecture.Core.Validation.FluentValidation.Tests.TestClass>;

namespace NArchitecture.Core.Validation.FluentValidation.Tests;

[Trait("Category", "Unit")]
public class FluentValidatorAdapterTests
{
    [Fact(DisplayName = "Validate should return valid result when validation passes")]
    public void Validate_ShouldReturnValidResult_WhenValidationPasses()
    {
        // Arrange
        var mockValidator = new Mock<FluentIValidator>();
        mockValidator.Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>())).Returns(new ValidationResult());

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);
        var instance = new TestClass();

        // Act
        var result = adapter.Validate(instance);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should return invalid result with errors when validation fails")]
    public void Validate_ShouldReturnInvalidResultWithErrors_WhenValidationFails()
    {
        // Arrange
        var mockValidator = new Mock<FluentIValidator>();
        var validationFailures = new List<ValidationFailure>
        {
            new("PropertyName1", "Error message 1"),
            new("PropertyName2", "Error message 2"),
        };
        mockValidator
            .Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>()))
            .Returns(new ValidationResult(validationFailures));

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);
        var instance = new TestClass();

        // Act
        var result = adapter.Validate(instance);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors!.Count().ShouldBe(2);
        result.Errors!.First().PropertyName.ShouldBe("PropertyName1");
        result.Errors!.First().Errors.First().ShouldBe("Error message 1");
        result.Errors!.Last().PropertyName.ShouldBe("PropertyName2");
        result.Errors!.Last().Errors.First().ShouldBe("Error message 2");
    }

    [Fact(DisplayName = "Validate should handle null instance gracefully")]
    public void Validate_ShouldHandleNullInstance_Gracefully()
    {
        // Arrange
        var mockValidator = new Mock<FluentIValidator>();
        mockValidator.Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>())).Throws<ArgumentNullException>();

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => adapter.Validate(null!));
    }

    [Fact(DisplayName = "Constructor should throw when validator is null")]
    public void Constructor_ShouldThrow_WhenValidatorIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FluentValidatorAdapter<TestClass>(null!));
    }
}

// Test class for validation
public class TestClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
