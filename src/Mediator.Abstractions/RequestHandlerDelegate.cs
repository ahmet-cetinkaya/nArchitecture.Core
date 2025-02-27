namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Represents the next delegate in the pipeline for requests with a response.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Response from the next delegate</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Represents the next delegate in the pipeline for requests without a response.
/// </summary>
public delegate Task RequestHandlerDelegate();
