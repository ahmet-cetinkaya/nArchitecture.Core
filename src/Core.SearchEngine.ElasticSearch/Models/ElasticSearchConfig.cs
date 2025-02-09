namespace NArchitecture.Core.SearchEngine.ElasticSearch.Models;

public record ElasticSearchConfig
{
    public string ConnectionString { get; init; } = string.Empty;
    public int NumberOfReplicas { get; init; } = 1;
    public int NumberOfShards { get; init; } = 3;
    public string Analyzer { get; init; } = "standard";
    public string MinimumShouldMatch { get; init; } = "30%";
    public float Boost { get; init; } = 1.1f;
    public int Fuzziness { get; init; } = 2;
    public int FuzzyMaxExpansions { get; init; } = 50;
    public int FuzzyPrefixLength { get; init; } = 0;
    public bool AnalyzeWildcard { get; init; } = true;
    public bool FuzzyTranspositions { get; init; } = true;
}
