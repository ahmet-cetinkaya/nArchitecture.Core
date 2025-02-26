namespace NArchitecture.Core.SearchEngine.ElasticSearch.Models;

/// <summary>
/// Configuration settings for ElasticSearch operations.
/// </summary>
/// <param name="ConnectionString">The connection string for the ElasticSearch server.</param>
public readonly record struct ElasticSearchConfig(string ConnectionString)
{
    /// <summary>
    /// Number of replica shards for each index. Default is 1.
    /// </summary>
    public int NumberOfReplicas { get; init; } = 1;

    /// <summary>
    /// Number of primary shards for each index. Default is 3.
    /// </summary>
    public int NumberOfShards { get; init; } = 3;

    /// <summary>
    /// The analyzer to use for text analysis. Default is "standard".
    /// </summary>
    public string Analyzer { get; init; } = "standard";

    /// <summary>
    /// Minimum number of terms that should match. Default is "30%".
    /// </summary>
    public string MinimumShouldMatch { get; init; } = "30%";

    /// <summary>
    /// Boost factor for search relevance. Default is 1.1.
    /// </summary>
    public float Boost { get; init; } = 1.1f;

    /// <summary>
    /// Maximum edit distance for fuzzy queries. Default is 2.
    /// </summary>
    public int Fuzziness { get; init; } = 2;

    /// <summary>
    /// Maximum number of terms to match in fuzzy queries. Default is 50.
    /// </summary>
    public int FuzzyMaxExpansions { get; init; } = 50;

    /// <summary>
    /// Length of common prefix for fuzzy queries. Default is 0.
    /// </summary>
    public int FuzzyPrefixLength { get; init; } = 0;

    /// <summary>
    /// Whether to analyze wildcard terms. Default is true.
    /// </summary>
    public bool AnalyzeWildcard { get; init; } = true;

    /// <summary>
    /// Whether to allow character transpositions in fuzzy queries. Default is true.
    /// </summary>
    public bool FuzzyTranspositions { get; init; } = true;
}
