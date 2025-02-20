# 🔍 NArchitecture Search Engine Abstractions

Essential search engine abstractions for Clean Architecture applications.

## ✨ Features

- 🔎 Provider-agnostic interface
- 📑 Document management
- 🎯 Index operations
- 🔄 CRUD operations
- ⚡ High-performance design

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.SearchEngine.Abstractions
```

## 🚦 Quick Start

```csharp
// Implement the search engine interface
public class ElasticSearchEngine : ISearchEngine
{
    public async Task<SearchResult> InsertAsync(SearchDocumentWithData document)
    {
        // Implementation for Elasticsearch
    }

    public async Task<List<SearchGetModel<T>>> GetSearchByField<T>(SearchByFieldParameters parameters)
        where T : class
    {
        // Implementation for Elasticsearch
    }

    // ... other implementations
}

// Register in DI
services.AddScoped<ISearchEngine, ElasticSearchEngine>();

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

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.SearchEngine.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
