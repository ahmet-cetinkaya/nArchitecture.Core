using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

public class ValidationException : System.Exception
{
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

    public ValidationException(IEnumerable<ValidationError> errors)
        : base(BuildErrorMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildErrorMessage(IEnumerable<ValidationError> errors)
    {
        IEnumerable<string> arr = errors.Select(x =>
            $"{Environment.NewLine} -- {x.PropertyName}: {string.Join(Environment.NewLine, values: x.Errors ?? Array.Empty<string>())}"
        );
        return $"Validation failed: {string.Join(string.Empty, arr)}";
    }
}
