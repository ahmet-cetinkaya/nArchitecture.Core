namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for streaming requests that return a stream of values of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Response type for each item in the stream</typeparam>
public interface IStreamRequest<out TResponse>;
