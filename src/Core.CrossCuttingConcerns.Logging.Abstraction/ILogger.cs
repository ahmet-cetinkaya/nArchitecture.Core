namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

/// <summary>
/// Defines asynchronous logging operations for different log levels.
/// </summary>
public interface ILogger
{
    Task TraceAsync(string message);
    Task CriticalAsync(string message);
    Task InformationAsync(string message);
    Task WarningAsync(string message);
    Task DebugAsync(string message);
    Task ErrorAsync(string message);
}
