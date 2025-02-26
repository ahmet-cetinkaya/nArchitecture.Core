namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

/// <summary>
/// Represents an index configuration model.
/// </summary>
/// <param name="IndexName">The name of the index.</param>
/// <param name="AliasName">The alias name for the index.</param>
/// <param name="IndexSettings">Optional settings for the index configuration.</param>
public readonly record struct IndexModel(string IndexName, string AliasName, IDictionary<string, object>? IndexSettings = null);
