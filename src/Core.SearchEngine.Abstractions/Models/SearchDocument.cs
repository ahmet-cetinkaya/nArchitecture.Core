namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

/// <summary>
/// Base record for search documents.
/// </summary>
/// <param name="Id">Unique identifier of the document.</param>
/// <param name="IndexName">Name of the index containing the document.</param>
public record SearchDocument(string Id, string IndexName);

/// <summary>
/// Search document that includes additional data.
/// </summary>
/// <param name="Id">Unique identifier of the document.</param>
/// <param name="IndexName">Name of the index containing the document.</param>
/// <param name="Data">The actual document data.</param>
public record SearchDocumentWithData(string Id, string IndexName, object Data) : SearchDocument(Id, IndexName);

/// <summary>
/// Model for retrieving search results with typed data.
/// </summary>
/// <typeparam name="T">Type of the item being retrieved.</typeparam>
public record SearchGetModel<T>(string Id, T Item, double Score)
    where T : class;
