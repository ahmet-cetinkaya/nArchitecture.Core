using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using NArchitecture.Core.SearchEngine.Abstractions;
using NArchitecture.Core.SearchEngine.Abstractions.Models;
using NArchitecture.Core.SearchEngine.ElasticSearch.Constants;
using NArchitecture.Core.SearchEngine.ElasticSearch.Models;

namespace NArchitecture.Core.SearchEngine.ElasticSearch;

public class ElasticSearchManager : ISearchEngine
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticSearchConfig _config;

    private const string DefaultIndex = "_all";
    private static readonly string[] AllIndices = [DefaultIndex];

    public ElasticSearchManager(ElasticSearchConfig configuration)
    {
        _config = configuration;
        var settings = new ElasticsearchClientSettings(new Uri(configuration.ConnectionString))
            .DefaultIndex(DefaultIndex)
            .EnableDebugMode()
            .PrettyJson()
            .DisableDirectStreaming()
            .ThrowExceptions();

        _client = new ElasticsearchClient(settings);
    }

    public async Task<SearchResult> CreateIndexAsync(IndexModel indexModel)
    {
        var existResponse = await _client.Indices.ExistsAsync(indexModel.IndexName);
        if (existResponse.Exists)
            return new SearchResult(false, Messages.IndexAlreadyExists);

        var response = await _client.Indices.CreateAsync(
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

    private SearchResult CreateResponse(bool isValid, string? errorReason = null) =>
        new(isValid, isValid ? Messages.Success : errorReason ?? Messages.UnknownError);

    public async Task<SearchResult> InsertAsync(SearchDocumentWithData document)
    {
        var response = await _client.IndexAsync(
            document.Data,
            i => i.Index(document.IndexName).Id(document.Id).Refresh(Refresh.True)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task<SearchResult> InsertManyAsync(string indexName, object[] items)
    {
        var bulkResponse = await _client.BulkAsync(b => b.Index(indexName).IndexMany(items));

        return CreateResponse(bulkResponse.IsValidResponse, bulkResponse.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task<IDictionary<string, object>> GetIndexList()
    {
        var response = await _client.Indices.GetAsync(AllIndices);
        return response.Indices.ToDictionary(x => x.Key.ToString(), x => (object)x.Value);
    }

    public async Task<List<SearchGetModel<T>>> GetAllSearch<T>(SearchParameters parameters)
        where T : class
    {
        var response = await _client.SearchAsync<T>(s =>
            s.Index(parameters.IndexName).From(parameters.From).Size(parameters.Size)
        );

        return response.Hits.Select(x => new SearchGetModel<T>(x.Id!, x.Source!, x.Score ?? 0.0)).ToList();
    }

    public async Task<List<SearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters parameters)
        where T : class
    {
        var response = await _client.SearchAsync<T>(s =>
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

    public async Task<List<SearchGetModel<T>>> GetSearchBySimpleQueryString<T>(SearchByQueryParameters parameters)
        where T : class
    {
        var response = await _client.SearchAsync<T>(s =>
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

    public async Task<SearchResult> UpdateAsync(SearchDocumentWithData document)
    {
        var response = await _client.UpdateAsync<object, object>(document.IndexName, document.Id, u => u.Doc(document.Data));

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task<SearchResult> UpdateByElasticIdAsync(SearchDocumentWithData model)
    {
        if (string.IsNullOrEmpty(model.IndexName))
            throw new ArgumentNullException(nameof(model.IndexName), Messages.IndexNameCannotBeNullOrEmpty);

        var response = await _client.UpdateAsync<object, object>(
            model.IndexName,
            model.Id,
            u => u.Doc(model.Data).RetryOnConflict(3)
        );

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task<SearchResult> DeleteAsync(SearchDocument document)
    {
        var response = await _client.DeleteAsync<object>(document.IndexName, document.Id);

        return CreateResponse(response.IsValidResponse, response.ElasticsearchServerError?.Error?.Reason);
    }
}
