namespace NArchitecture.Core.Validation.Abstractions;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <param name="IsValid">Indicates whether the validation was successful.</param>
/// <param name="Errors">Collection of validation errors if validation failed, null otherwise.</param>
public record ValidationResult(bool IsValid, IEnumerable<ValidationError>? Errors);
