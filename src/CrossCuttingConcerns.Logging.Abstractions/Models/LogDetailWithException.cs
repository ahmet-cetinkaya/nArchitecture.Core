namespace NArchitecture.Core.CrossCuttingConcerns.Logging;

/// <summary>
/// Extends LogDetail to include exception information for error logging.
/// </summary>
public class LogDetailWithException : LogDetail
{
    /// <summary>
    /// Gets or sets the exception message that occurred during the operation.
    /// </summary>
    public string ExceptionMessage { get; set; }

    public LogDetailWithException()
    {
        ExceptionMessage = string.Empty;
    }

    public LogDetailWithException(
        string fullName,
        string methodName,
        string user,
        List<LogParameter> parameters,
        string exceptionMessage
    )
        : base(fullName, methodName, user, parameters)
    {
        ExceptionMessage = exceptionMessage;
    }
}
