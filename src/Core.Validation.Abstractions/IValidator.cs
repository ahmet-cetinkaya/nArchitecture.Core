namespace NArchitecture.Core.Validation.Abstractions;

/// <summary>
/// Provides an abstraction for validating instances of a specific type.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates an instance of <typeparamref name="T"/> and returns the result.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> that indicates if validation succeeded along with error messages if any.</returns>
    ValidationResult Validate(T instance);
}
