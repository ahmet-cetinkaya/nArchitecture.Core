namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

/// <summary>
/// Represents errors that occur when a requested resource is not found.
/// </summary>
public class NotFoundException : System.Exception
{
    public NotFoundException() { }

    public NotFoundException(string? message)
        : base(message) { }

    public NotFoundException(string? message, System.Exception? innerException)
        : base(message, innerException) { }
}
