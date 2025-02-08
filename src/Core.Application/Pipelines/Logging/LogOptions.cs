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

    public LogOptions()
    {
        ExcludeParameters = Array.Empty<LogExcludeParameter>();
        LogResponse = false;
    }

    public LogOptions(params LogExcludeParameter[] excludeParameters)
    {
        ExcludeParameters = excludeParameters;
        LogResponse = false;
    }

    public LogOptions(bool logResponse, params LogExcludeParameter[] excludeParameters)
    {
        ExcludeParameters = excludeParameters;
        LogResponse = logResponse;
    }

    public static readonly LogOptions Default = new();
}
