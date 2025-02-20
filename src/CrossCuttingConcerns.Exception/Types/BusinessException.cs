namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

/// <summary>
/// Represents errors that occur during business logic operations.
/// </summary>
public class BusinessException : System.Exception
{
    public BusinessException() { }

    public BusinessException(string? message)
        : base(message) { }

    public BusinessException(string? message, System.Exception? innerException)
        : base(message, innerException) { }
}
