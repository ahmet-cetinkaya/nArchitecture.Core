using NArchitecture.Core.SearchEngine.Abstractions.Models;

namespace NArchitecture.Core.SearchEngine.Abstractions;

public interface ISearchEngine
{
    Task<SearchResult> CreateIndexAsync(IndexModel indexModel);
    Task<SearchResult> InsertAsync(SearchDocumentWithData document);
    Task<SearchResult> InsertManyAsync(string indexName, object[] items);
    Task<IDictionary<string, object>> GetIndexList();

    Task<List<SearchGetModel<T>>> GetAllSearch<T>(SearchParameters parameters)
        where T : class;

    Task<List<SearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters fieldParameters)
        where T : class;

    Task<List<SearchGetModel<T>>> GetSearchBySimpleQueryString<T>(SearchByQueryParameters queryParameters)
        where T : class;

    Task<SearchResult> UpdateAsync(SearchDocumentWithData document);
    Task<SearchResult> DeleteAsync(SearchDocument document);
}
