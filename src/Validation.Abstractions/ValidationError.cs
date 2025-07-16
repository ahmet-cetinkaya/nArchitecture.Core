namespace NArchitecture.Core.Validation.Abstractions;

/// <summary>
/// Represents a validation error with property name and error message.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="Errors">Collection of error messages for the property.</param>
public record ValidationError(string PropertyName, IEnumerable<string> Errors);
