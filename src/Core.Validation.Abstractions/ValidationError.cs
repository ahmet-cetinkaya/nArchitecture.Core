using System.Text.Json.Serialization;

namespace NArchitecture.Core.Validation.Abstractions;

/// <summary>
/// Represents a validation error with property name and error message.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; init; }

    /// <summary>
    /// Gets the error message describing the validation failure.
    /// </summary>
    public IEnumerable<string> Errors { get; init; }

    public ValidationError()
    {
        PropertyName = string.Empty;
        Errors = Array.Empty<string>();
    }

    public ValidationError(string propertyName, IEnumerable<string> errors)
    {
        PropertyName = propertyName;
        Errors = errors;
    }

    public ValidationError(string propertyName, string error)
        : this(propertyName, new[] { error }) { }
}
