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
        // Arrange: Setup mock validator and create a test instance.
        var mockValidator = new Mock<FluentIValidator>();
        _ = mockValidator.Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>())).Returns(new ValidationResult());

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);
        var instance = new TestClass();

        // Act: Call Validate method.
        Abstractions.ValidationResult result = adapter.Validate(instance);

        // Assert: Verify the result is valid and error-free.
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should return invalid result with errors when validation fails")]
    public void Validate_ShouldReturnInvalidResultWithErrors_WhenValidationFails()
    {
        // Arrange: Setup mock validator to produce validation errors.
        var mockValidator = new Mock<FluentIValidator>();
        var validationFailures = new List<ValidationFailure>
        {
            new("PropertyName1", "Error message 1"),
            new("PropertyName2", "Error message 2"),
        };
        _ = mockValidator
            .Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>()))
            .Returns(new ValidationResult(validationFailures));

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);
        var instance = new TestClass();

        // Act: Call Validate method.
        Abstractions.ValidationResult result = adapter.Validate(instance);

        // Assert: Verify the result contains the expected errors.
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
        // Arrange: Configure mock to throw when instance is null.
        var mockValidator = new Mock<FluentIValidator>();
        _ = mockValidator.Setup(v => v.Validate(It.IsAny<ValidationContext<TestClass>>())).Throws<ArgumentNullException>();

        var adapter = new FluentValidatorAdapter<TestClass>(mockValidator.Object);

        // Act & Assert: Expect ArgumentNullException.
        _ = Should.Throw<ArgumentNullException>(() => adapter.Validate(null!));
    }

    [Fact(DisplayName = "Constructor should throw when validator is null")]
    public void Constructor_ShouldThrow_WhenValidatorIsNull()
    {
        // Act & Assert: Verify constructor throws when passed a null validator.
        _ = Should.Throw<ArgumentNullException>(() => new FluentValidatorAdapter<TestClass>(null!));
    }
}

// Test class for validation
public class TestClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
