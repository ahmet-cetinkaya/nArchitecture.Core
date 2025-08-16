namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Defines the mediator interface for sending requests and publishing events.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request to a single handler and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the request handler</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request that has no return value to a single handler.
    /// </summary>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming request to a single handler and returns a stream of responses.
    /// </summary>
    /// <typeparam name="TResponse">Response type for each item in the stream</typeparam>
    /// <param name="request">Streaming request object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of responses from the request handler</returns>
    IAsyncEnumerable<TResponse> SendStreamAsync<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes a evemt to all registered handlers.
    /// </summary>
    /// <param name="event">Event instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}
