namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Pipeline behavior for processing streaming requests.
/// </summary>
/// <typeparam name="TRequest">The streaming request type</typeparam>
/// <typeparam name="TResponse">The response type for each item in the stream</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Process the streaming request through the pipeline.
    /// </summary>
    /// <param name="request">The streaming request being processed</param>
    /// <param name="next">The delegate for the next action in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of responses from the pipeline</returns>
    IAsyncEnumerable<TResponse> Handle(
        TRequest request,
        RequestStreamHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    );
}
