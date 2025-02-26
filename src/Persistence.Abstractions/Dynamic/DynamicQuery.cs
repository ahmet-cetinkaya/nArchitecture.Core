namespace NArchitecture.Core.Persistence.Abstractions.Dynamic;

/// <summary>
/// Represents a dynamic query containing optional sort and filter criteria.
/// </summary>
/// <param name="Sort">The collection of sort criteria.</param>
/// <param name="Filter">The filter criteria.</param>
public readonly record struct DynamicQuery(Filter? Filter = null, IEnumerable<Sort>? Sort = null);
