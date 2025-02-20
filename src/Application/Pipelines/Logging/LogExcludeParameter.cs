namespace NArchitecture.Core.Application.Pipelines.Logging;

/// <summary>
/// Represents a parameter to be excluded from logging.
/// </summary>
public readonly struct LogExcludeParameter
{
    /// <summary>
    /// Gets the name of the parameter to exclude.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets whether to mask the value instead of removing it completely.
    /// </summary>
    public bool Mask { get; init; }

    /// <summary>
    /// Gets the mask character to use if masking is enabled.
    /// Default is '*'.
    /// </summary>
    public char MaskChar { get; init; }

    /// <summary>
    /// Gets the number of characters to keep visible at the start if masking is enabled.
    /// Default is 0.
    /// </summary>
    public int KeepStartChars { get; init; }

    /// <summary>
    /// Gets the number of characters to keep visible at the end if masking is enabled.
    /// Default is 0.
    /// </summary>
    public int KeepEndChars { get; init; }

    public LogExcludeParameter(string name, bool mask = false, char maskChar = '*', int keepStartChars = 0, int keepEndChars = 0)
    {
        Name = name;
        Mask = mask;
        MaskChar = maskChar;
        KeepStartChars = keepStartChars;
        KeepEndChars = keepEndChars;
    }

    public static implicit operator LogExcludeParameter(string name) => new(name);
}
