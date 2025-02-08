using Serilog;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File;

/// <summary>
/// Implements file-based logging using Serilog with configurable settings.
/// </summary>
public class SerilogFileLogger(SerilogFileLogConfiguration configuration)
    : SerilogLoggerServiceBase(
        // Configure and create Serilog logger instance with file sink
        new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: $"{Directory.GetCurrentDirectory() + configuration.FolderPath}.log",
                rollingInterval: configuration.RollingInterval,
                retainedFileCountLimit: configuration.RetainedFileCountLimit,
                fileSizeLimitBytes: configuration.FileSizeLimitBytes,
                outputTemplate: configuration.OutputTemplate,
                buffered: true
            )
            .CreateLogger()
    ) { }
