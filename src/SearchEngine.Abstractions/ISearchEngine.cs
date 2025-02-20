using NArchitecture.Core.SearchEngine.Abstractions.Models;

namespace NArchitecture.Core.SearchEngine.Abstractions;

/// <summary>
/// Defines the interface for search engine operations.
/// </summary>
public interface ISearchEngine
{
    /// <summary>
    /// Creates a new index with the specified configuration.
    /// </summary>
    /// <param name="indexModel">The index configuration model.</param>
    Task<SearchResult> CreateIndexAsync(IndexModel indexModel);

    /// <summary>
    /// Inserts a single document into the search index.
    /// </summary>
    /// <param name="document">The document to insert.</param>
    Task<SearchResult> InsertAsync(SearchDocumentWithData document);

    /// <summary>
    /// Inserts multiple items into the specified index.
    /// </summary>
    /// <param name="indexName">The target index name.</param>
    /// <param name="items">Array of items to insert.</param>
    Task<SearchResult> InsertManyAsync(string indexName, object[] items);

    /// <summary>
    /// Retrieves a list of all available indices.
    /// </summary>
    /// <returns>A dictionary containing index information.</returns>
    Task<IDictionary<string, object>> GetIndexList();

    /// <summary>
    /// Performs a search query to retrieve all documents of type T from the specified index.
    /// </summary>
    /// <typeparam name="T">The type of documents to retrieve.</typeparam>
    /// <param name="parameters">Search parameters including pagination.</param>
    Task<List<SearchGetModel<T>>> GetAllSearch<T>(SearchParameters parameters)
        where T : class;

    /// <summary>
    /// Searches for documents by matching a specific field value.
    /// </summary>
    /// <typeparam name="T">The type of documents to retrieve.</typeparam>
    /// <param name="fieldParameters">Parameters specifying the field and value to search for.</param>
    Task<List<SearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters fieldParameters)
        where T : class;

    /// <summary>
    /// Searches for documents using a simple query string.
    /// </summary>
    /// <typeparam name="T">The type of documents to retrieve.</typeparam>
    /// <param name="queryParameters">Parameters containing the query string to search with.</param>
    Task<List<SearchGetModel<T>>> GetSearchBySimpleQueryString<T>(SearchByQueryParameters queryParameters)
        where T : class;

    /// <summary>
    /// Updates an existing document in the search index.
    /// </summary>
    /// <param name="document">The document with updated data.</param>
    Task<SearchResult> UpdateAsync(SearchDocumentWithData document);

    /// <summary>
    /// Deletes a document from the search index.
    /// </summary>
    /// <param name="document">The document to delete.</param>
    Task<SearchResult> DeleteAsync(SearchDocument document);
}
