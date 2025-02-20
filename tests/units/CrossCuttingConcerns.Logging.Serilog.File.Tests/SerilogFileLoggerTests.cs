using Serilog;
using Shouldly;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File.Tests;

public class SerilogFileLoggerTests : IDisposable
{
    private readonly string _baseTestPath;
    private readonly List<string> _testPaths;

    /// <summary>
    /// Initializes test environment by creating a temporary log directory and configuring the logger.
    /// </summary>
    public SerilogFileLoggerTests()
    {
        _baseTestPath = Path.Combine(Path.GetTempPath(), $"narch_tests_{Guid.NewGuid()}");
        _testPaths = [];
        _ = Directory.CreateDirectory(_baseTestPath);
    }

    private (string path, SerilogFileLogger logger) createTestLogger(string testName)
    {
        // Clean up any existing test paths before creating new one
        cleanupTestPaths();

        string testPath = Path.Combine(_baseTestPath, $"test_{testName}_{Guid.NewGuid()}");
        _testPaths.Add(testPath);

        try
        {
            _ = Directory.CreateDirectory(testPath);
            var config = new SerilogFileLogConfiguration(
                folderPath: testPath,
                rollingInterval: RollingInterval.Infinite,
                fileSizeLimitBytes: 50,
                retainedFileCountLimit: 2
            );

            return (testPath, new SerilogFileLogger(config));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to setup test logger for {testName} at {testPath}", ex);
        }
    }

    private void cleanupTestPaths()
    {
        foreach (string path in _testPaths.ToList())
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    _ = _testPaths.Remove(path);
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to cleanup test path {path}: {ex.Message}");
            }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        _ = Directory.CreateDirectory(path);

        if (!Directory.Exists(path))
            throw new InvalidOperationException($"Failed to create directory: {path}");
    }

    private static void VerifyDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new InvalidOperationException($"Directory does not exist: {path}");
    }

    private static async Task WaitForFileCreation(string path, int maxWaitMs = 1000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < maxWaitMs)
        {
            if (Directory.Exists(path) && Directory.GetFiles(path, "*.log").Length > 0)
                return;
            await Task.Delay(50);
        }

        throw new TimeoutException($"Timeout waiting for log file creation in {path}");
    }

    /// <summary>
    /// Verifies that InformationAsync correctly writes messages to the log file.
    /// </summary>
    [Fact]
    public async Task InformationAsync_ShouldWriteToLogFile()
    {
        // Arrange
        (string testPath, SerilogFileLogger logger) = createTestLogger(nameof(InformationAsync_ShouldWriteToLogFile));
        string testMessage = "Test information message";

        // Act
        await logger.InformationAsync(testMessage);
        await Task.Delay(100); // Add delay to ensure file is written

        // Assert
        string[] logFiles = Directory.GetFiles(testPath, "*.log");
        logFiles.Length.ShouldBeGreaterThan(0);
        string logContent = await System.IO.File.ReadAllTextAsync(logFiles[0]);
        logContent.ShouldContain(testMessage);
    }

    /// <summary>
    /// Verifies that all logging methods correctly write messages with appropriate log levels.
    /// </summary>
    /// <param name="message">The test message to log</param>
    /// <param name="methodName">The name of the logging method to test</param>
    [Theory]
    [InlineData("Debug test message", nameof(SerilogFileLogger.DebugAsync))]
    [InlineData("Error test message", nameof(SerilogFileLogger.ErrorAsync))]
    [InlineData("Warning test message", nameof(SerilogFileLogger.WarningAsync))]
    [InlineData("Critical test message", nameof(SerilogFileLogger.CriticalAsync))]
    [InlineData("Trace test message", nameof(SerilogFileLogger.TraceAsync))]
    public async Task LogMethods_ShouldWriteCorrectLogLevel(string message, string methodName)
    {
        // Arrange
        (string testPath, SerilogFileLogger logger) = createTestLogger(
            $"{nameof(LogMethods_ShouldWriteCorrectLogLevel)}_{methodName}"
        );
        System.Reflection.MethodInfo? method = typeof(SerilogFileLogger).GetMethod(methodName);

        // Act
        await (Task)method!.Invoke(logger, new object[] { message })!;
        await Task.Delay(500); // Increased delay for file creation

        // Assert
        string[] logFiles = Directory.GetFiles(testPath, "*.log");
        string filesInfo = string.Join("\n", logFiles.Select(f => $"{f}: {new FileInfo(f).Length} bytes"));
        logFiles.Length.ShouldBeGreaterThan(
            0,
            $"No log files were created. Directory: {testPath}\nFiles info:\n{filesInfo}\nTesting method: {methodName}"
        );

        if (logFiles.Length > 0)
        {
            string logContent = await System.IO.File.ReadAllTextAsync(logFiles[0]);
            logContent.ShouldContain(message, customMessage: $"Log content was: {logContent}\nTesting method: {methodName}");
        }
    }

    /// <summary>
    /// Verifies that the logger creates new log files when size limit is reached.
    /// </summary>
    [Fact]
    public async Task Logger_ShouldRespectFileSizeLimit()
    {
        // Arrange
        (string testPath, SerilogFileLogger logger) = createTestLogger(nameof(Logger_ShouldRespectFileSizeLimit));
        Directory.Exists(testPath).ShouldBeTrue($"Test directory was not created: {testPath}");

        string largeMessage = new('x', 150);

        try
        {
            // Act
            for (int i = 0; i < 15; i++)
            {
                await logger.InformationAsync($"{i}:{largeMessage}");
                await Task.Delay(10);
            }

            // Assert
            await Task.Delay(500);
            Directory.Exists(testPath).ShouldBeTrue($"Test directory was deleted during test: {testPath}");

            string[] logFiles = Directory.GetFiles(testPath, "*.log");
            string filesInfo = string.Join("\n", logFiles.Select(f => $"{f}: {new FileInfo(f).Length} bytes"));
            logFiles.Length.ShouldBeGreaterThan(
                1,
                $"Expected multiple log files but found {logFiles.Length}.\nPath: {testPath}\nFiles info:\n{filesInfo}"
            );
        }
        finally
        {
            // Cleanup this specific test's resources
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }
    }

    /// <summary>
    /// Verifies that the logger maintains the configured maximum number of log files.
    /// </summary>
    [Fact]
    public async Task Logger_ShouldRespectRetainedFileLimit()
    {
        // Arrange
        (string testPath, SerilogFileLogger logger) = createTestLogger(nameof(Logger_ShouldRespectRetainedFileLimit));
        string largeMessage = new('x', 40); // Message size close to file size limit

        // Act
        for (int i = 0; i < 10; i++) // Write enough to create multiple files
        {
            await logger.InformationAsync($"{i}:{largeMessage}");
            await Task.Delay(50);
        }

        // Assert
        await Task.Delay(200); // Wait for file operations
        string[] logFiles = Directory.GetFiles(testPath, "*.log");
        string filesInfo = string.Join("\n", logFiles.Select(f => $"{f}: {new FileInfo(f).Length} bytes"));
        logFiles.Length.ShouldBeLessThanOrEqualTo(
            2,
            $"Expected 2 or fewer files but found {logFiles.Length}.\nFiles info:\n{filesInfo}"
        );
    }

    /// <summary>
    /// Cleans up test resources by removing the temporary log directory.
    /// </summary>
    public void Dispose()
    {
        cleanupTestPaths();

        try
        {
            if (Directory.Exists(_baseTestPath))
                Directory.Delete(_baseTestPath, true);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to cleanup base test path {_baseTestPath}: {ex.Message}");
        }
    }
}
