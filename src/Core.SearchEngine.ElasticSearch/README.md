# 🔍 NArchitecture Elasticsearch Integration

High-performance Elasticsearch integration for Clean Architecture applications.

## ✨ Features

- 🔎 Full-text search
- 📊 Fuzzy matching
- 🎯 Query customization
- 🔄 Bulk operations
- ⚡ High-performance design

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.SearchEngine.ElasticSearch
```

## 🚦 Quick Start

```csharp
// Configure Elasticsearch
var config = new ElasticSearchConfig
{
    ConnectionString = "http://localhost:9200",
    NumberOfReplicas = 1,
    NumberOfShards = 3,
    Analyzer = "standard",
    Fuzziness = 2
};

// Register in DI
services.AddSingleton(config);
services.AddScoped<ISearchEngine, ElasticSearchManager>();

// Usage
public class DocumentService
{
    private readonly ISearchEngine _searchEngine;

    public DocumentService(ISearchEngine searchEngine)
    {
        _searchEngine = searchEngine;
    }

    public async Task<List<SearchGetModel<Document>>> SearchDocuments(string query)
    {
        var parameters = new SearchByQueryParameters(
            indexName: "documents",
            queryString: query,
            size: 10
        );

        return await _searchEngine.GetSearchBySimpleQueryString<Document>(parameters);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.SearchEngine.ElasticSearch)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
