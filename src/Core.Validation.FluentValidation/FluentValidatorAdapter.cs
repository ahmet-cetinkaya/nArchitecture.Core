using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.Validation.FluentValidation;

/// <summary>
/// Adapts FluentValidation validators to work with the application's validation interface.
/// </summary>
/// <typeparam name="T">Type of the object to validate.</typeparam>
public class FluentValidatorAdapter<T> : NArchitecture.Core.Validation.Abstractions.IValidator<T>
    where T : class
{
    private readonly global::FluentValidation.IValidator<T> _fluentValidator;

    /// <summary>
    /// Creates a new instance of FluentValidatorAdapter.
    /// </summary>
    /// <param name="fluentValidator">The FluentValidation validator to adapt.</param>
    public FluentValidatorAdapter(global::FluentValidation.IValidator<T> fluentValidator)
    {
        _fluentValidator = fluentValidator;
    }

    /// <summary>
    /// Validates the specified instance using FluentValidation.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>Validation result containing any validation errors.</returns>
    public ValidationResult Validate(T instance)
    {
        var context = new global::FluentValidation.ValidationContext<T>(instance);
        var result = _fluentValidator.Validate(context);

        return new ValidationResult
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)),
        };
    }
}
