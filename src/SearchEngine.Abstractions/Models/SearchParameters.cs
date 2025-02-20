namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

/// <summary>
/// Basic search parameters for querying data.
/// </summary>
public readonly struct SearchParameters(string indexName, int from = 0, int size = 10)
{
    /// <summary>The name of the index to search in.</summary>
    public string IndexName { get; init; } = indexName;

    /// <summary>Starting position of the search results.</summary>
    public int From { get; init; } = from;

    /// <summary>Maximum number of items to return.</summary>
    public int Size { get; init; } = size;
}

/// <summary>
/// Parameters for searching by a specific field value.
/// </summary>
public readonly struct SearchByFieldParameters(string indexName, string fieldName, string value, int from = 0, int size = 10)
{
    /// <inheritdoc cref="SearchParameters.IndexName"/>
    public string IndexName { get; init; } = indexName;

    /// <inheritdoc cref="SearchParameters.From"/>
    public int From { get; init; } = from;

    /// <inheritdoc cref="SearchParameters.Size"/>
    public int Size { get; init; } = size;

    /// <summary>The name of the field to search in.</summary>
    public string FieldName { get; init; } = fieldName;

    /// <summary>The value to search for in the specified field.</summary>
    public string Value { get; init; } = value;
}

/// <summary>
/// Parameters for searching using a query string.
/// </summary>
public readonly struct SearchByQueryParameters(string indexName, string queryString, int from = 0, int size = 10)
{
    /// <inheritdoc cref="SearchParameters.IndexName"/>
    public string IndexName { get; init; } = indexName;

    /// <inheritdoc cref="SearchParameters.From"/>
    public int From { get; init; } = from;

    /// <inheritdoc cref="SearchParameters.Size"/>
    public int Size { get; init; } = size;

    /// <summary>The query string to search with.</summary>
    public string QueryString { get; init; } = queryString;
}
