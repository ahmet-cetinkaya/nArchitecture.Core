using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

/// <summary>
/// Represents errors that occur during validation operations.
/// </summary>
public class ValidationException : System.Exception
{
    /// <summary>
    /// Gets the collection of validation errors that caused the exception.
    /// </summary>
    public IEnumerable<ValidationError> Errors { get; }

    public ValidationException()
        : base()
    {
        Errors = Array.Empty<ValidationError>();
    }

    public ValidationException(string? message)
        : base(message)
    {
        Errors = Array.Empty<ValidationError>();
    }

    public ValidationException(string? message, System.Exception? innerException)
        : base(message, innerException)
    {
        Errors = Array.Empty<ValidationError>();
    }

    /// <summary>
    /// Initializes a new instance with the specified validation errors.
    /// </summary>
    /// <param name="errors">The collection of validation errors.</param>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base(BuildErrorMessage(errors))
    {
        Errors = errors;
    }

    // Formats the error message by combining all validation errors
    private static string BuildErrorMessage(IEnumerable<ValidationError> errors)
    {
        IEnumerable<string> arr = errors.Select(x =>
            $"{Environment.NewLine} -- {x.PropertyName}: {string.Join(Environment.NewLine, values: x.Errors ?? Array.Empty<string>())}"
        );
        return $"Validation failed: {string.Join(string.Empty, arr)}";
    }
}
