namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions.Models;

/// <summary>
/// Represents detailed information about a logging event.
/// </summary>
public class LogDetail
{
    /// <summary>
    /// Gets or sets the full name of the logged component.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the name of the method where the log was created.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Gets or sets the user associated with the logged operation.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets the collection of parameters involved in the logged operation.
    /// </summary>
    public List<LogParameter> Parameters { get; set; }

    public LogDetail()
    {
        FullName = string.Empty;
        MethodName = string.Empty;
        User = string.Empty;
        Parameters = [];
    }

    public LogDetail(string fullName, string methodName, string user, List<LogParameter> parameters)
    {
        FullName = fullName;
        MethodName = methodName;
        User = user;
        Parameters = parameters;
    }
}
