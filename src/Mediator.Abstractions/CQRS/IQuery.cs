namespace NArchitecture.Core.Mediator.Abstractions.CQRS;

/// <summary>
/// Represents a query that returns a value of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
