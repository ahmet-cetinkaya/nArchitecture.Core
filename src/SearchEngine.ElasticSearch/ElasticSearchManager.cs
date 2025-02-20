using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using NArchitecture.Core.SearchEngine.Abstractions;
using NArchitecture.Core.SearchEngine.Abstractions.Models;
using NArchitecture.Core.SearchEngine.ElasticSearch.Constants;
using NArchitecture.Core.SearchEngine.ElasticSearch.Models;

namespace NArchitecture.Core.SearchEngine.ElasticSearch;

/// <summary>
/// Implementation of ISearchEngine for Elasticsearch operations.
/// </summary>
public class ElasticSearchManager : ISearchEngine
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticSearchConfig _config;

    private const string DefaultIndex = "_all";
    private static readonly string[] AllIndices = [DefaultIndex];

    /// <summary>
    /// Initializes a new instance of ElasticSearchManager with the specified configuration.
    /// </summary>
    /// <param name="configuration">Elasticsearch configuration settings.</param>
    public ElasticSearchManager(ElasticSearchConfig configuration)
    {
        _config = configuration;
        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(configuration.ConnectionString))
            .DefaultIndex(DefaultIndex)
            .EnableDebugMode()
            .PrettyJson()
            .DisableDirectStreaming()
            .ThrowExceptions();

        _client = new ElasticsearchClient(settings);
    }

    /// <inheritdoc/>
    public async Task<SearchResult> CreateIndexAsync(IndexModel indexModel)
    {
        Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse existResponse = await _client.Indices.ExistsAsync(
            indexModel.IndexName
        );
        if (existResponse.Exists)
            return new SearchResult(false, Messages.IndexAlreadyExists);

        Elastic.Clients.Elasticsearch.IndexManagement.CreateIndexResponse response = await _client.Indices.CreateAsync(
            indexModel.IndexName,
            i =>
                i.Settings(s => s.NumberOfReplicas(_config.NumberOfReplicas).NumberOfShards(_config.NumberOfShards))
                    .Aliases(a => a.Add(indexModel.AliasName, descriptor => { }))
        );

        return new SearchResult(
            response.IsValidResponse,
            response.IsValidResponse
                ? Messages.Success
                : response.ElasticsearchServerError?.Error?.Reason ?? Messages.UnknownError
        );
    }

    /// <summary>
    /// Creates a standardized search response.
    /// </summary>
    /// <param name="isValid">Indicates if the operation was successful.</param>
    /// <param name="errorReason">Optional error reason if operation failed.</param>
    private static SearchResult CreateResponse(bool isValid, string? errorReason = null)
    {
        return new(isValid, isValid ? Messages.Success : errorReason ?? Messages.UnknownError);
    }

    /// <inheritdoc/>
    public async Task<SearchResult> InsertAsync(SearchDocumentWithData document)
    {
        IndexResponse response = await _client.IndexAsync(
            document.Data,
            i => i.Index(document.IndexName).Id(document.Id).Refresh(Refresh.True)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    /// <inheritdoc/>
    public async Task<SearchResult> InsertManyAsync(string indexName, object[] items)
    {
        BulkResponse bulkResponse = await _client.BulkAsync(b => b.Index(indexName).IndexMany(items).Refresh(Refresh.True));

        return CreateResponse(bulkResponse.IsValidResponse, bulkResponse.ElasticsearchServerError?.Error?.Reason);
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, object>> GetIndexList()
    {
        Elastic.Clients.Elasticsearch.IndexManagement.GetIndexResponse response = await _client.Indices.GetAsync(AllIndices);
        return response.Indices.ToDictionary(x => x.Key.ToString(), x => (object)x.Value);
    }

    /// <inheritdoc/>
    public async Task<List<SearchGetModel<T>>> GetAllSearch<T>(SearchParameters parameters)
        where T : class
    {
        SearchResponse<T> response = await _client.SearchAsync<T>(s =>
            s.Index(parameters.IndexName).From(parameters.From).Size(parameters.Size)
        );

        return response.Hits.Select(x => new SearchGetModel<T>(x.Id!, x.Source!, x.Score ?? 0.0)).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<SearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters parameters)
        where T : class
    {
        SearchResponse<T> response = await _client.SearchAsync<T>(s =>
            s.Index(parameters.IndexName)
                .From(parameters.From)
                .Size(parameters.Size)
                .Query(q =>
                    q.Match(m =>
                        m.Field(parameters.FieldName!)
                            .Query(parameters.Value)
                            .Analyzer(_config.Analyzer)
                            .Fuzziness(new Fuzziness(_config.Fuzziness))
                            .MinimumShouldMatch(_config.MinimumShouldMatch)
                            .Boost(_config.Boost)
                            .Lenient()
                            .FuzzyTranspositions(_config.FuzzyTranspositions)
                            .MaxExpansions(_config.FuzzyMaxExpansions)
                            .PrefixLength(_config.FuzzyPrefixLength)
                    )
                )
        );

        return response.Hits.Select(x => new SearchGetModel<T>(x.Id!, x.Source!, x.Score ?? 0.0)).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<SearchGetModel<T>>> GetSearchBySimpleQueryString<T>(SearchByQueryParameters parameters)
        where T : class
    {
        SearchResponse<T> response = await _client.SearchAsync<T>(s =>
            s.Index(parameters.IndexName)
                .From(parameters.From)
                .Size(parameters.Size)
                .Query(q =>
                    q.SimpleQueryString(qs =>
                        qs.Query(parameters.QueryString)
                            .DefaultOperator(Operator.Or)
                            .AnalyzeWildcard(_config.AnalyzeWildcard)
                            .Analyzer(_config.Analyzer)
                            .MinimumShouldMatch(_config.MinimumShouldMatch)
                            .FuzzyTranspositions(_config.FuzzyTranspositions)
                            .FuzzyMaxExpansions(_config.FuzzyMaxExpansions)
                            .FuzzyPrefixLength(_config.FuzzyPrefixLength)
                            .Boost(_config.Boost)
                    )
                )
        );

        return response.Hits.Select(x => new SearchGetModel<T>(x.Id!, x.Source!, x.Score ?? 0.0)).ToList();
    }

    /// <inheritdoc/>
    public async Task<SearchResult> UpdateAsync(SearchDocumentWithData document)
    {
        UpdateResponse<object> response = await _client.UpdateAsync<object, object>(
            document.IndexName,
            document.Id,
            u => u.Doc(document.Data).Refresh(Refresh.True)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    /// <summary>
    /// Updates a document by its Elasticsearch ID with retry functionality.
    /// </summary>
    /// <param name="model">The document with updated data.</param>
    /// <exception cref="ArgumentNullException">Thrown when index name is null or empty.</exception>
    public async Task<SearchResult> UpdateByElasticIdAsync(SearchDocumentWithData model)
    {
        if (string.IsNullOrEmpty(model.IndexName))
            throw new ArgumentNullException(nameof(model), Messages.IndexNameCannotBeNullOrEmpty);

        UpdateResponse<object> response = await _client.UpdateAsync<object, object>(
            model.IndexName,
            model.Id,
            u => u.Doc(model.Data).Refresh(Refresh.True).RetryOnConflict(3)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    /// <inheritdoc/>
    public async Task<SearchResult> DeleteAsync(SearchDocument document)
    {
        DeleteResponse response = await _client.DeleteAsync<object>(
            document.IndexName,
            document.Id,
            d => d.Refresh(Refresh.True)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }
}
