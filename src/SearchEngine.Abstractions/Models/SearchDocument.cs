namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

/// <summary>
/// Base record for search documents.
/// </summary>
/// <param name="Id">Unique identifier of the document.</param>
/// <param name="IndexName">Name of the index containing the document.</param>
public readonly record struct SearchDocument(string Id, string IndexName);

/// <summary>
/// Search document that includes additional data.
/// </summary>
/// <param name="Id">Unique identifier of the document.</param>
/// <param name="IndexName">Name of the index containing the document.</param>
/// <param name="Data">The actual document data.</param>
public readonly record struct SearchDocumentWithData(string Id, string IndexName, object Data);

/// <summary>
/// Model for retrieving search results with typed data.
/// </summary>
/// <typeparam name="T">Type of the item being retrieved.</typeparam>
public readonly record struct SearchGetModel<T>(string Id, T Item, double Score)
    where T : class;
