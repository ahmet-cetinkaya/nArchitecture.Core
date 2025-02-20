namespace NArchitecture.Core.SearchEngine.ElasticSearch.Constants;

/// <summary>
/// Contains constant message strings used in ElasticSearch operations.
/// </summary>
public static class Messages
{
    /// <summary>Operation completed successfully message.</summary>
    public const string Success = "Operation completed successfully.";

    /// <summary>Unknown error occurred message.</summary>
    public const string UnknownError = "An unknown error occurred.";

    /// <summary>Index already exists message.</summary>
    public const string IndexAlreadyExists = "Index already exists.";

    /// <summary>Index name cannot be null or empty message.</summary>
    public const string IndexNameCannotBeNullOrEmpty = "Index name cannot be null or empty.";

    /// <summary>Document not found message.</summary>
    public const string DocumentNotFound = "Document not found.";

    /// <summary>Index not found message.</summary>
    public const string IndexNotFound = "Index not found.";

    /// <summary>Invalid query message.</summary>
    public const string InvalidQuery = "Invalid query.";
}
