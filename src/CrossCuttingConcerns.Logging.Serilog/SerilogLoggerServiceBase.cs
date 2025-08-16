using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;
using PackageSerilog = Serilog;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog;

/// <summary>
/// Base class for Serilog logging implementations that provides common logging functionality.
/// </summary>
/// <remarks>
/// Note: For true asynchronous logging, configure Serilog with async sinks to prevent blocking application threads.
/// Example: .WriteTo.Async(a => a.File(...)) or .WriteTo.Async(a => a.Console())
/// This ensures log messages are processed on background threads, improving application performance.
/// </remarks>
public abstract class SerilogLoggerServiceBase(PackageSerilog.ILogger logger) : ILogger
{
    /// <summary>
    /// The Serilog logger instance used for logging operations.
    /// </summary>
    protected readonly PackageSerilog.ILogger Logger = logger;

    /// <summary>
    /// Logs a critical message that represents a critical error that requires immediate attention.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task CriticalAsync(string message)
    {
        Logger.Fatal(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs a debug message that is used for development and troubleshooting.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task DebugAsync(string message)
    {
        Logger.Debug(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs an error message that represents an error that has occurred.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task ErrorAsync(string message)
    {
        Logger.Error(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs an informational message that provides general information.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task InformationAsync(string message)
    {
        Logger.Information(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs a trace message that is used for detailed tracing of the application's execution.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task TraceAsync(string message)
    {
        Logger.Verbose(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs a warning message that represents a potential issue or important situation.
    /// </summary>
    /// <param name="message">The message to be logged</param>
    public Task WarningAsync(string message)
    {
        Logger.Warning(message);
        return Task.CompletedTask;
    }
}
