namespace NArchitecture.Core.Persistence.Abstractions.Dynamic;

/// <summary>
/// Represents the sorting criteria for dynamic queries.
/// </summary>
/// <param name="Field">The field to sort by.</param>
/// <param name="Dir">The sorting direction (e.g., "asc" or "desc").</param>
public readonly record struct Sort(string Field, string Dir);
