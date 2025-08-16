namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for requests that don't return a value.
/// </summary>
public interface IRequest;

/// <summary>
/// Marker interface for requests that return a value of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse>;
