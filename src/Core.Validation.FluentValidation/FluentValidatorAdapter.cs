using FluentValidation;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.Validation.FluentValidation;

public class FluentValidatorAdapter<T> : NArchitecture.Core.Validation.Abstractions.IValidator<T>
    where T : class
{
    private readonly global::FluentValidation.IValidator<T> _fluentValidator;

    public FluentValidatorAdapter(global::FluentValidation.IValidator<T> fluentValidator)
    {
        _fluentValidator = fluentValidator;
    }

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
