namespace NArchitecture.Core.Mediator.Abstractions.CQRS;

/// <summary>
/// Represents a command that doesn't return a value.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Represents a command that returns a value of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
