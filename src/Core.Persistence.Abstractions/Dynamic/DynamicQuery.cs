namespace NArchitecture.Core.Persistence.Abstractions.Dynamic;

/// <summary>
/// Represents a dynamic query containing optional sort and filter criteria.
/// </summary>
public record DynamicQuery(IEnumerable<Sort>? Sort = null, Filter? Filter = null)
{
    /// <summary>
    /// Gets or sets the collection of sort criteria.
    /// </summary>
    public IEnumerable<Sort>? Sort { get; set; } = Sort;

    /// <summary>
    /// Gets or sets the filter criteria.
    /// </summary>
    public Filter? Filter { get; set; } = Filter;
}
