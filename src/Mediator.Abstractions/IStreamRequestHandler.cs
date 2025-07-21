namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Defines a handler for a streaming request that returns a stream of responses of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">Streaming request type</typeparam>
/// <typeparam name="TResponse">Response type for each item in the stream</typeparam>
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the streaming request and returns an async enumerable of responses.
    /// </summary>
    /// <param name="request">The streaming request to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of responses</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
