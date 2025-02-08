namespace NArchitecture.Core.Application.Pipelines.Logging;

/// <summary>
/// Represents logging options for a request.
/// </summary>
public readonly struct LogOptions
{
    /// <summary>
    /// Gets the parameters to exclude or mask from logging.
    /// </summary>
    public LogExcludeParameter[] ExcludeParameters { get; init; }

    /// <summary>
    /// Gets whether to log successful responses.
    /// </summary>
    public bool LogResponse { get; init; }

    /// <summary>
    /// Gets the user information to be included in logs.
    /// </summary>
    public string User { get; init; }

    public LogOptions()
    {
        ExcludeParameters = Array.Empty<LogExcludeParameter>();
        LogResponse = false;
        User = "?";
    }

    public LogOptions(params LogExcludeParameter[] excludeParameters)
    {
        ExcludeParameters = excludeParameters;
        LogResponse = false;
        User = "?";
    }

    public LogOptions(bool logResponse, params LogExcludeParameter[] excludeParameters)
    {
        ExcludeParameters = excludeParameters;
        LogResponse = logResponse;
        User = "?";
    }

    public LogOptions(string? user, bool logResponse = false, params LogExcludeParameter[] excludeParameters)
    {
        ExcludeParameters = excludeParameters;
        LogResponse = logResponse;
        User = string.IsNullOrEmpty(user) ? "?" : user;
    }

    public static readonly LogOptions Default = new();
}
