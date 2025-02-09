namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

public record IndexModel(string IndexName, string AliasName, IDictionary<string, object>? IndexSettings = null);
