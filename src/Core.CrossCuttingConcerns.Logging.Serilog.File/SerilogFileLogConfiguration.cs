using Serilog;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File;

/// <summary>
/// Configuration settings for Serilog file-based logging implementation.
/// Provides customization options for log file management and formatting.
/// </summary>
public readonly struct SerilogFileLogConfiguration(
    string folderPath,
    RollingInterval rollingInterval = RollingInterval.Day,
    long? fileSizeLimitBytes = 5_000_000,
    string? outputTemplate = null,
    int? retainedFileCountLimit = null
)
{
    // Default template for structured log entries
    private const string DEFAULT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

    /// <summary>
    /// Gets the directory path where log files will be stored.
    /// The path is relative to the application's current directory.
    /// </summary>
    public string FolderPath { get; init; } = folderPath;

    /// <summary>
    /// Gets the interval at which new log files should be created.
    /// Controls the frequency of log file rotation.
    /// </summary>
    public RollingInterval RollingInterval { get; init; } = rollingInterval;

    /// <summary>
    /// Gets the maximum size limit for each log file in bytes.
    /// When exceeded, a new log file will be created.
    /// </summary>
    public long? FileSizeLimitBytes { get; init; } = fileSizeLimitBytes;

    /// <summary>
    /// Gets the template string that defines the format of log entries.
    /// If not specified, uses the default template.
    /// </summary>
    public string OutputTemplate { get; init; } = outputTemplate ?? DEFAULT_TEMPLATE;

    /// <summary>
    /// Gets the maximum number of log files to keep.
    /// Oldest files will be deleted when this limit is exceeded.
    /// </summary>
    public int? RetainedFileCountLimit { get; init; } = retainedFileCountLimit;
}
