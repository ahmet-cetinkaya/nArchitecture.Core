namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

/// <summary>
/// Defines a contract for asynchronous logging operations at different severity levels.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs detailed trace information useful for debugging.
    /// </summary>
    /// <param name="message">The trace message to log</param>
    Task TraceAsync(string message);

    /// <summary>
    /// Logs critical errors that require immediate attention.
    /// </summary>
    /// <param name="message">The critical error message to log</param>
    Task CriticalAsync(string message);

    /// <summary>
    /// Logs informational messages about application flow.
    /// </summary>
    /// <param name="message">The informational message to log</param>
    Task InformationAsync(string message);

    /// <summary>
    /// Logs warning messages about potentially harmful situations.
    /// </summary>
    /// <param name="message">The warning message to log</param>
    Task WarningAsync(string message);

    /// <summary>
    /// Logs debug information useful during development.
    /// </summary>
    /// <param name="message">The debug message to log</param>
    Task DebugAsync(string message);

    /// <summary>
    /// Logs error messages for application failures.
    /// </summary>
    /// <param name="message">The error message to log</param>
    Task ErrorAsync(string message);
}
