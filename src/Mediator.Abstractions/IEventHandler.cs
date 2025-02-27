namespace NArchitecture.Core.Mediator.Abstractions;

/// <summary>
/// Defines a handler for a event.
/// </summary>
/// <typeparam name="TEvent">Event type</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="event">The event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TEvent @event, CancellationToken cancellationToken);
}
