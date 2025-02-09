namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

public record SearchDocument(string Id, string IndexName);

public record SearchDocumentWithData(string Id, string IndexName, object Data) : SearchDocument(Id, IndexName);

public record SearchGetModel<T>(string Id, T Item, double Score)
    where T : class;
