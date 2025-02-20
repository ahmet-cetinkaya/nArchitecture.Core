namespace NArchitecture.Core.Application.Pipelines.Logging;

/// <summary>
/// Marker interface for requests that should be logged by the <see cref="LoggingBehavior{TRequest, TResponse}"/>.
/// Implement this interface on requests that need to be logged.
/// </summary>
public interface ILoggableRequest
{
    /// <summary>
    /// Gets the logging options for the request.
    /// </summary>
    LogOptions LogOptions => LogOptions.Default;
}
