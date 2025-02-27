namespace NArchitecture.Core.Mediator.Abstractions.CQRS;

/// <summary>
/// Defines a handler for a command that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

/// <summary>
/// Defines a handler for a command that doesn't return a value.
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;
