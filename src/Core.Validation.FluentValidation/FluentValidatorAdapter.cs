using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.Validation.FluentValidation;

/// <summary>
/// Provides an adapter implementation for FluentValidation validators to work with the application's validation interface.
/// </summary>
/// <typeparam name="T">The type of object to be validated.</typeparam>
public class FluentValidatorAdapter<T>(global::FluentValidation.IValidator<T> fluentValidator) : IValidator<T>
    where T : class
{
    /// <inheritdoc/>
    public ValidationResult Validate(T instance)
    {
        // Create validation context and execute FluentValidation
        var context = new global::FluentValidation.ValidationContext<T>(instance);
        var result = fluentValidator.Validate(context);

        // Map FluentValidation result to application's ValidationResult
        return new ValidationResult
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(e => new ValidationError(e.PropertyName, [e.ErrorMessage])),
        };
    }
}
