using NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog;
using Serilog;
using Serilog.Events;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File;

/// <summary>
/// Implements file-based logging using Serilog with configurable settings.
/// Handles log file rotation, size limits, and formatting based on provided configuration.
/// </summary>
public class SerilogFileLogger(SerilogFileLogConfiguration configuration)
    : SerilogLoggerServiceBase(
        new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(
                path: Path.Combine(configuration.FolderPath, "log.log"),
                rollingInterval: configuration.RollingInterval,
                retainedFileCountLimit: configuration.RetainedFileCountLimit,
                fileSizeLimitBytes: configuration.FileSizeLimitBytes,
                outputTemplate: configuration.OutputTemplate,
                buffered: false,
                shared: false,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogEventLevel.Verbose
            )
            .CreateLogger()
    ) { }
