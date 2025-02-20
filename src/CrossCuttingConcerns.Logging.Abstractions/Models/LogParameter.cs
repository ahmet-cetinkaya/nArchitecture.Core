namespace NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions.Models;

/// <summary>
/// Represents a parameter information for logging purposes.
/// </summary>
public class LogParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the parameter.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets or sets the type name of the parameter.
    /// </summary>
    public string Type { get; set; }

    public LogParameter()
    {
        Name = string.Empty;
        Value = string.Empty;
        Type = string.Empty;
    }

    public LogParameter(string name, object value, string type)
    {
        Name = name;
        Value = value;
        Type = type;
    }
}
