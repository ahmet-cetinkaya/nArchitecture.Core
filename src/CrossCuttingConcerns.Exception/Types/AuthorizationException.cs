namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

/// <summary>
/// Represents errors that occur during authorization operations.
/// </summary>
public class AuthorizationException : System.Exception
{
    public AuthorizationException() { }

    public AuthorizationException(string? message)
        : base(message) { }

    public AuthorizationException(string? message, System.Exception? innerException)
        : base(message, innerException) { }
}
