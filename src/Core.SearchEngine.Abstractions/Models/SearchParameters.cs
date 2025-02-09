namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

public readonly struct SearchParameters(string indexName, int from = 0, int size = 10)
{
    public string IndexName { get; init; } = indexName;
    public int From { get; init; } = from;
    public int Size { get; init; } = size;
}

public readonly struct SearchByFieldParameters(string indexName, string fieldName, string value, int from = 0, int size = 10)
{
    public string IndexName { get; init; } = indexName;
    public int From { get; init; } = from;
    public int Size { get; init; } = size;
    public string FieldName { get; init; } = fieldName;
    public string Value { get; init; } = value;
}

public readonly struct SearchByQueryParameters(string indexName, string queryString, int from = 0, int size = 10)
{
    public string IndexName { get; init; } = indexName;
    public int From { get; init; } = from;
    public int Size { get; init; } = size;
    public string QueryString { get; init; } = queryString;
}
