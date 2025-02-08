namespace NArchitecture.Core.Validation.Abstractions;

/// <summary>
/// Represents a validation error with property name and error message.
/// </summary>
public readonly struct ValidationError
{
    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; init; }

    /// <summary>
    /// Gets the error message describing the validation failure.
    /// </summary>
    public string ErrorMessage { get; init; }

    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}
