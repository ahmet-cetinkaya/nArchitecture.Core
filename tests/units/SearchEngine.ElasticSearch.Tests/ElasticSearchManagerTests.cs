using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Elastic.Clients.Elasticsearch;
using NArchitecture.Core.SearchEngine.Abstractions.Models;
using NArchitecture.Core.SearchEngine.ElasticSearch.Constants;
using NArchitecture.Core.SearchEngine.ElasticSearch.Models;
using Shouldly;

namespace NArchitecture.Core.SearchEngine.ElasticSearch.Tests;

[CollectionDefinition("ElasticSearch", DisableParallelization = true)]
public class ElasticSearchCollection : ICollectionFixture<ElasticSearchFixture> { }

public class TestDocument
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ElasticSearchFixture : IAsyncLifetime
{
    private readonly string _containerName;
    public ElasticSearchManager Manager { get; private set; } = null!;
    private readonly DockerClient _dockerClient;
    private string? _containerId;
    private const string TestIndex = "test-index";
    private const string TestAlias = "test-alias";
    private ElasticsearchClient _client = null!;
    private readonly int _httpPort;
    private readonly int _transportPort;
    private bool _skipContainerCreation;
    private string _elasticHost = null!;

    public ElasticSearchFixture()
    {
        _containerName = $"elasticsearch-test-{Guid.NewGuid():N}";
        _httpPort = GetRandomPort();
        _transportPort = GetRandomPort();

        Uri dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
        _dockerClient = new DockerClientConfiguration(dockerUri).CreateClient();
    }

    private async Task InitializeManager()
    {
        _elasticHost = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? $"http://localhost:{_httpPort}";

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELASTICSEARCH_URL")))
            await InitializeLocalContainer();
        else
            _skipContainerCreation = true;

        var config = new ElasticSearchConfig
        {
            ConnectionString = _elasticHost,
            NumberOfReplicas = 1,
            NumberOfShards = 3,
            Analyzer = "standard",
            Fuzziness = 1,
            MinimumShouldMatch = "2<70%",
            Boost = 1.0f,
            FuzzyTranspositions = true,
            FuzzyMaxExpansions = 50,
            FuzzyPrefixLength = 0,
            AnalyzeWildcard = true,
        };

        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(config.ConnectionString))
            .DefaultIndex("_all")
            .EnableDebugMode()
            .PrettyJson()
            .DisableDirectStreaming()
            .ThrowExceptions();

        Manager = new ElasticSearchManager(config);
        _client = new ElasticsearchClient(settings);
    }

    private static int GetRandomPort()
    {
        var random = new Random();
        return random.Next(10000, 60000);
    }

    private async Task CleanupExistingContainers()
    {
        try
        {
            IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }
            );

            foreach (
                ContainerListResponse? container in containers.Where(c => c.Names.Any(n => n.Contains("elasticsearch-test-")))
            )
                try
                {
                    _ = await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
                }
                catch { }
        }
        catch { }
    }

    private async Task InitializeLocalContainer()
    {
        await CleanupExistingContainers();

        CreateContainerResponse createContainerResponse = await _dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = "docker.elastic.co/elasticsearch/elasticsearch:8.9.2",
                Name = _containerName,
                ExposedPorts = new Dictionary<string, EmptyStruct> { { "9200/tcp", default }, { "9300/tcp", default } },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "9200/tcp",
                            new List<PortBinding> { new() { HostPort = _httpPort.ToString() } }
                        },
                        {
                            "9300/tcp",
                            new List<PortBinding> { new() { HostPort = _transportPort.ToString() } }
                        },
                    },
                    Memory = 2147483648,
                    MemorySwap = 4294967296,
                },
                Env = new[] { "discovery.type=single-node", "xpack.security.enabled=false", "ES_JAVA_OPTS=-Xms512m -Xmx512m" },
            }
        );

        _containerId = createContainerResponse.ID;
        _ = await _dockerClient.Containers.StartContainerAsync(_containerId, null);
    }

    public async Task InitializeAsync()
    {
        await InitializeManager();

        if (!_skipContainerCreation)
        {
            using var httpClient = new HttpClient();
            int maxAttempts = 60;
            int attempt = 0;
            while (attempt < maxAttempts)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:{_httpPort}/_cluster/health");
                    if (response.IsSuccessStatusCode)
                    {
                        await Task.Delay(5000);
                        break;
                    }
                }
                catch { }

                attempt++;
                await Task.Delay(2000);
            }

            if (attempt >= maxAttempts)
                throw new Exception("Elasticsearch failed to start within the expected timeframe.");
        }
        else
        {
            using var httpClient = new HttpClient();
            int maxAttempts = 30;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync($"{_elasticHost}/_cluster/health");
                    if (response.IsSuccessStatusCode)
                        break;
                }
                catch { }

                attempt++;
                await Task.Delay(1000);
            }

            if (attempt >= maxAttempts)
                throw new Exception("Could not connect to Elasticsearch service.");
        }

        _ = await Manager.CreateIndexAsync(new IndexModel(TestIndex, TestAlias));
        await Task.Delay(1000);

        _ = await Manager.InsertAsync(
            new SearchDocumentWithData(
                Id: "1",
                IndexName: TestIndex,
                Data: new TestDocument { Name = "Test", Description = "Sample Description" }
            )
        );
        await Task.Delay(1000);
    }

    public async Task ResetIndexAsync()
    {
        try
        {
            _ = await _client.Indices.DeleteAsync(TestIndex);
            await Task.Delay(1000);
        }
        catch { }

        await Task.Delay(1000);

        _ = await Manager.CreateIndexAsync(new IndexModel(TestIndex, TestAlias));
        await Task.Delay(1000);

        _ = await Manager.InsertAsync(
            new SearchDocumentWithData(
                Id: "1",
                IndexName: TestIndex,
                Data: new TestDocument { Name = "Test", Description = "Sample Description" }
            )
        );
        await Task.Delay(1000);
    }

    public async Task DisposeAsync()
    {
        if (!_skipContainerCreation && _containerId != null)
        {
            _ = await _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters());
            await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters());
        }
    }
}

[Collection("ElasticSearch")]
public class ElasticSearchManagerTests
{
    private readonly ElasticSearchManager _manager;
    private readonly ElasticSearchFixture _fixture;
    private const string TestIndex = "test-index";
    private const string TestAlias = "test-alias";

    public ElasticSearchManagerTests(ElasticSearchFixture fixture)
    {
        _fixture = fixture;
        _manager = fixture.Manager;
    }

    private Task ResetTestDataAsync()
    {
        return _fixture.ResetIndexAsync();
    }

    [Fact(DisplayName = "CreateIndexAsync should return error when index exists")]
    [Trait("Category", "ElasticSearch")]
    public async Task CreateIndexAsync_WhenIndexExists_ShouldReturnError()
    {
        await ResetTestDataAsync();
        var indexModel = new IndexModel(TestIndex, TestAlias);

        SearchResult result = await _manager.CreateIndexAsync(indexModel);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe(Messages.IndexAlreadyExists);
    }

    [Fact(DisplayName = "GetAllSearch should return documents from index")]
    [Trait("Category", "ElasticSearch")]
    public async Task GetAllSearch_ShouldReturnDocuments()
    {
        await ResetTestDataAsync();
        var parameters = new SearchParameters
        {
            IndexName = TestIndex,
            From = 0,
            Size = 10,
        };

        List<SearchGetModel<TestDocument>> result = await _manager.GetAllSearch<TestDocument>(parameters);

        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result[0].Item.Name.ShouldBe("Test");
    }

    [Theory(DisplayName = "GetSearchByField should return matching documents based on field value")]
    [InlineData("name", "Test")]
    [InlineData("description", "Sample")]
    [Trait("Category", "ElasticSearch")]
    public async Task GetSearchByField_WithValidParameters_ShouldReturnMatchingDocuments(string fieldName, string value)
    {
        await ResetTestDataAsync();
        var parameters = new SearchByFieldParameters
        {
            IndexName = TestIndex,
            FieldName = fieldName,
            Value = value,
            From = 0,
            Size = 10,
        };

        List<SearchGetModel<TestDocument>> result = await _manager.GetSearchByField<TestDocument>(parameters);

        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        _ = result[0].Item.ShouldBeOfType<TestDocument>();
    }

    [Fact(DisplayName = "UpdateAsync should update a valid document successfully")]
    [Trait("Category", "ElasticSearch")]
    public async Task UpdateAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        await ResetTestDataAsync();
        var document = new SearchDocumentWithData(
            Id: "1",
            IndexName: TestIndex,
            Data: new TestDocument { Name = "Updated Test", Description = "Updated Description" }
        );

        SearchResult result = await _manager.UpdateAsync(document);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);

        List<SearchGetModel<TestDocument>> updated = await _manager.GetAllSearch<TestDocument>(
            new SearchParameters
            {
                IndexName = TestIndex,
                From = 0,
                Size = 10,
            }
        );
        updated[0].Item.Name.ShouldBe("Updated Test");
    }

    [Fact(DisplayName = "DeleteAsync should delete a valid document successfully")]
    [Trait("Category", "ElasticSearch")]
    public async Task DeleteAsync_WithValidDocument_ShouldDeleteSuccessfully()
    {
        await ResetTestDataAsync();
        var document = new SearchDocument("1", TestIndex);

        SearchResult result = await _manager.DeleteAsync(document);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);

        List<SearchGetModel<TestDocument>> searchResult = await _manager.GetAllSearch<TestDocument>(
            new SearchParameters
            {
                IndexName = TestIndex,
                From = 0,
                Size = 10,
            }
        );
        searchResult.ShouldBeEmpty();
    }

    [Fact(DisplayName = "CreateIndexAsync should create index when it does not exist")]
    [Trait("Category", "ElasticSearch")]
    public async Task CreateIndexAsync_WhenIndexDoesNotExist_ShouldCreateSuccessfully()
    {
        string uniqueIndex = $"new-test-index-{Guid.NewGuid():N}";
        var indexModel = new IndexModel(uniqueIndex, "test-alias");

        SearchResult result = await _manager.CreateIndexAsync(indexModel);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);
    }

    [Fact(DisplayName = "InsertManyAsync should insert multiple documents successfully")]
    [Trait("Category", "ElasticSearch")]
    public async Task InsertManyAsync_WithValidDocuments_ShouldInsertSuccessfully()
    {
        await ResetTestDataAsync();
        TestDocument[] documents =
        [
            new TestDocument { Name = "Test1", Description = "Description1" },
            new TestDocument { Name = "Test2", Description = "Description2" },
        ];

        SearchResult result = await _manager.InsertManyAsync(TestIndex, documents);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);

        List<SearchGetModel<TestDocument>> searchResult = await _manager.GetAllSearch<TestDocument>(
            new SearchParameters
            {
                IndexName = TestIndex,
                From = 0,
                Size = 10,
            }
        );
        searchResult.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "GetIndexList should return all indices")]
    [Trait("Category", "ElasticSearch")]
    public async Task GetIndexList_ShouldReturnAllIndices()
    {
        await ResetTestDataAsync();

        IDictionary<string, object> result = await _manager.GetIndexList();

        _ = result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
        result.ShouldContainKey(TestIndex);
    }

    [Fact(DisplayName = "GetSearchBySimpleQueryString should return matching documents")]
    [Trait("Category", "ElasticSearch")]
    public async Task GetSearchBySimpleQueryString_ShouldReturnMatchingDocuments()
    {
        await ResetTestDataAsync();
        var parameters = new SearchByQueryParameters
        {
            IndexName = TestIndex,
            QueryString = "Test",
            From = 0,
            Size = 10,
        };

        List<SearchGetModel<TestDocument>> result = await _manager.GetSearchBySimpleQueryString<TestDocument>(parameters);

        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result[0].Item.Name.ShouldBe("Test");
    }

    [Fact(DisplayName = "UpdateByElasticIdAsync should update document using ElasticId successfully")]
    [Trait("Category", "ElasticSearch")]
    public async Task UpdateByElasticIdAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        await ResetTestDataAsync();
        var document = new SearchDocumentWithData(
            Id: "1",
            IndexName: TestIndex,
            Data: new TestDocument { Name = "Updated Via ElasticId", Description = "Updated Description" }
        );

        SearchResult result = await _manager.UpdateByElasticIdAsync(document);

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);

        List<SearchGetModel<TestDocument>> updated = await _manager.GetAllSearch<TestDocument>(
            new SearchParameters
            {
                IndexName = TestIndex,
                From = 0,
                Size = 10,
            }
        );
        updated[0].Item.Name.ShouldBe("Updated Via ElasticId");
    }

    [Fact(DisplayName = "UpdateByElasticIdAsync should throw exception when index name is null")]
    [Trait("Category", "ElasticSearch")]
    public async Task UpdateByElasticIdAsync_WithNullIndexName_ShouldThrowArgumentNullException()
    {
        var document = new SearchDocumentWithData(Id: "1", IndexName: "", Data: new TestDocument());

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await _manager.UpdateByElasticIdAsync(document));
    }
}
