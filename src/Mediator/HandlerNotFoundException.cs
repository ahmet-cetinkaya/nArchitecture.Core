namespace NArchitecture.Core.Mediator;

/// <summary>
/// Exception thrown when a handler is not found for a request or event.
/// </summary>
public class HandlerNotFoundException : Exception
{
    /// <summary>
    /// Gets the type of the request or event that has no handler.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerNotFoundException"/> class.
    /// </summary>
    /// <param name="requestType">The type of the request or event that has no handler.</param>
    public HandlerNotFoundException(Type requestType)
        : base($"No handler registered for {requestType.Name}")
    {
        RequestType = requestType;
    }
}
