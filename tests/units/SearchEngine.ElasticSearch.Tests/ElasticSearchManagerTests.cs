using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Elastic.Clients.Elasticsearch;
using NArchitecture.Core.SearchEngine.Abstractions.Models;
using NArchitecture.Core.SearchEngine.ElasticSearch.Constants;
using NArchitecture.Core.SearchEngine.ElasticSearch.Models;
using Shouldly;

namespace NArchitecture.Core.SearchEngine.ElasticSearch.Tests;

/// <summary>
/// Collection definition for ElasticSearch tests to disable parallel execution.
/// </summary>
[CollectionDefinition("ElasticSearch", DisableParallelization = true)]
public class ElasticSearchCollection : ICollectionFixture<ElasticSearchFixture> { }

/// <summary>
/// Sample document class for testing purposes.
/// </summary>
public class TestDocument
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Test fixture that manages the ElasticSearch Docker container and test data.
/// </summary>
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
        // Get the host from environment or use localhost
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

    /// <summary>
    /// Gets a random available port number for Docker container binding.
    /// </summary>
    private static int GetRandomPort()
    {
        var random = new Random();
        return random.Next(10000, 60000);
    }

    /// <summary>
    /// Cleans up any existing test containers from previous runs.
    /// </summary>
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
                catch
                {
                    // Ignore errors during cleanup
                }
        }
        catch
        {
            // Ignore errors during cleanup
        }
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

    /// <summary>
    /// Initializes the test environment by starting ElasticSearch container and creating test data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await InitializeManager();

        if (!_skipContainerCreation)
        {
            // Local environment health check
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
                catch
                {
                    // Ignore exceptions while waiting
                }

                attempt++;
                await Task.Delay(2000);
            }

            if (attempt >= maxAttempts)
                throw new Exception("Elasticsearch failed to start within the expected timeframe.");
        }
        else
        {
            // CI environment health check
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
                catch
                {
                    // Ignore exceptions while waiting
                }

                attempt++;
                await Task.Delay(1000);
            }

            if (attempt >= maxAttempts)
                throw new Exception("Could not connect to Elasticsearch service.");
        }

        // Create test index and insert test data
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

    /// <summary>
    /// Resets the test index to a known state with initial test data.
    /// </summary>
    public async Task ResetIndexAsync()
    {
        try
        {
            _ = await _client.Indices.DeleteAsync(TestIndex);
            await Task.Delay(1000); // Increased delay for better stability
        }
        catch
        {
            // Ignore if index doesn't exist
        }

        await Task.Delay(1000); // Additional delay before creating new index

        // Create fresh index
        _ = await Manager.CreateIndexAsync(new IndexModel(TestIndex, TestAlias));
        await Task.Delay(1000);

        // Insert initial test document
        _ = await Manager.InsertAsync(
            new SearchDocumentWithData(
                Id: "1",
                IndexName: TestIndex,
                Data: new TestDocument { Name = "Test", Description = "Sample Description" }
            )
        );
        await Task.Delay(1000);
    }

    /// <summary>
    /// Cleans up resources by stopping and removing the Docker container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (!_skipContainerCreation && _containerId != null)
        {
            _ = await _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters());
            await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters());
        }
    }
}

/// <summary>
/// Integration tests for ElasticSearchManager implementation.
/// </summary>
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

    /// <summary>
    /// Resets the test data to its initial state.
    /// </summary>
    private Task ResetTestDataAsync()
    {
        return _fixture.ResetIndexAsync();
    }

    /// <summary>
    /// Tests that attempt to create an index that already exists returns appropriate error.
    /// </summary>
    [Fact]
    public async Task CreateIndexAsync_WhenIndexExists_ShouldReturnError()
    {
        // Arrange
        await ResetTestDataAsync();
        var indexModel = new IndexModel(TestIndex, TestAlias);

        // Act
        SearchResult result = await _manager.CreateIndexAsync(indexModel);

        // Assert
        result.Success.ShouldBeFalse();
        result.Message.ShouldBe(Messages.IndexAlreadyExists);
    }

    /// <summary>
    /// Tests that GetAllSearch returns all documents from the specified index.
    /// </summary>
    [Fact]
    public async Task GetAllSearch_ShouldReturnDocuments()
    {
        // Arrange
        await ResetTestDataAsync();
        var parameters = new SearchParameters
        {
            IndexName = TestIndex,
            From = 0,
            Size = 10,
        };

        // Act
        List<SearchGetModel<TestDocument>> result = await _manager.GetAllSearch<TestDocument>(parameters);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result[0].Item.Name.ShouldBe("Test");
    }

    /// <summary>
    /// Tests that search by field returns documents matching the specified field value.
    /// </summary>
    [Theory]
    [InlineData("name", "Test")]
    [InlineData("description", "Sample")]
    public async Task GetSearchByField_WithValidParameters_ShouldReturnMatchingDocuments(string fieldName, string value)
    {
        // Arrange
        await ResetTestDataAsync();
        var parameters = new SearchByFieldParameters
        {
            IndexName = TestIndex,
            FieldName = fieldName,
            Value = value,
            From = 0,
            Size = 10,
        };

        // Act
        List<SearchGetModel<TestDocument>> result = await _manager.GetSearchByField<TestDocument>(parameters);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        _ = result[0].Item.ShouldBeOfType<TestDocument>();
    }

    /// <summary>
    /// Tests that document update operation works successfully.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        // Arrange
        await ResetTestDataAsync();
        var document = new SearchDocumentWithData(
            Id: "1",
            IndexName: TestIndex,
            Data: new TestDocument { Name = "Updated Test", Description = "Updated Description" }
        );

        // Act
        SearchResult result = await _manager.UpdateAsync(document);

        // Assert
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

    /// <summary>
    /// Tests that document deletion operation works successfully.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithValidDocument_ShouldDeleteSuccessfully()
    {
        // Arrange
        await ResetTestDataAsync();
        var document = new SearchDocument("1", TestIndex);

        // Act
        SearchResult result = await _manager.DeleteAsync(document);

        // Assert
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

    /// <summary>
    /// Tests that new index creation works when index doesn't exist.
    /// </summary>
    [Fact]
    public async Task CreateIndexAsync_WhenIndexDoesNotExist_ShouldCreateSuccessfully()
    {
        // Arrange
        string uniqueIndex = $"new-test-index-{Guid.NewGuid():N}";
        var indexModel = new IndexModel(uniqueIndex, "test-alias");

        // Act
        SearchResult result = await _manager.CreateIndexAsync(indexModel);

        // Assert
        result.Success.ShouldBeTrue();
        result.Message.ShouldBe(Messages.Success);
    }

    /// <summary>
    /// Tests that bulk insert operation works correctly for multiple documents.
    /// </summary>
    [Fact]
    public async Task InsertManyAsync_WithValidDocuments_ShouldInsertSuccessfully()
    {
        // Arrange
        await ResetTestDataAsync();
        TestDocument[] documents = new[]
        {
            new TestDocument { Name = "Test1", Description = "Description1" },
            new TestDocument { Name = "Test2", Description = "Description2" },
        };

        // Act
        SearchResult result = await _manager.InsertManyAsync(TestIndex, documents);

        // Assert
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
        searchResult.Count.ShouldBe(3); // 1 existing + 2 new documents
    }

    /// <summary>
    /// Tests that GetIndexList returns all available indices.
    /// </summary>
    [Fact]
    public async Task GetIndexList_ShouldReturnAllIndices()
    {
        // Arrange
        await ResetTestDataAsync();

        // Act
        IDictionary<string, object> result = await _manager.GetIndexList();

        // Assert
        _ = result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
        result.ShouldContainKey(TestIndex);
    }

    /// <summary>
    /// Tests that simple query string search returns matching documents.
    /// </summary>
    [Fact]
    public async Task GetSearchBySimpleQueryString_ShouldReturnMatchingDocuments()
    {
        // Arrange
        await ResetTestDataAsync();
        var parameters = new SearchByQueryParameters
        {
            IndexName = TestIndex,
            QueryString = "Test",
            From = 0,
            Size = 10,
        };

        // Act
        List<SearchGetModel<TestDocument>> result = await _manager.GetSearchBySimpleQueryString<TestDocument>(parameters);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result[0].Item.Name.ShouldBe("Test");
    }

    /// <summary>
    /// Tests that document update by ElasticId works successfully.
    /// </summary>
    [Fact]
    public async Task UpdateByElasticIdAsync_WithValidDocument_ShouldUpdateSuccessfully()
    {
        // Arrange
        await ResetTestDataAsync();
        var document = new SearchDocumentWithData(
            Id: "1",
            IndexName: TestIndex,
            Data: new TestDocument { Name = "Updated Via ElasticId", Description = "Updated Description" }
        );

        // Act
        SearchResult result = await _manager.UpdateByElasticIdAsync(document);

        // Assert
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

    /// <summary>
    /// Tests that updating document with null index name throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task UpdateByElasticIdAsync_WithNullIndexName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var document = new SearchDocumentWithData(Id: "1", IndexName: "", Data: new TestDocument());

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await _manager.UpdateByElasticIdAsync(document));
    }
}
