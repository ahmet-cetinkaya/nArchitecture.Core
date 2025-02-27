namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Pipeline behavior for processing requests with a response.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Process the request through the pipeline.
    /// </summary>
    /// <param name="request">The request being processed</param>
    /// <param name="next">The delegate for the next action in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the pipeline</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for processing requests without a response.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
public interface IPipelineBehavior<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Process the request through the pipeline.
    /// </summary>
    /// <param name="request">The request being processed</param>
    /// <param name="next">The delegate for the next action in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken);
}
