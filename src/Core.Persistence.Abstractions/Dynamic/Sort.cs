namespace NArchitecture.Core.Persistence.Abstractions.Dynamic;

/// <summary>
/// Represents the sorting criteria for dynamic queries.
/// </summary>
public record Sort(string Field, string Dir)
{
    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    public string Field { get; set; } = Field;

    /// <summary>
    /// Gets or sets the sorting direction (e.g., "asc" or "desc").
    /// </summary>
    public string Dir { get; set; } = Dir;
}
