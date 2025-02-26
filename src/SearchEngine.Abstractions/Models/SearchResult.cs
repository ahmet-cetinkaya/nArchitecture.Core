namespace NArchitecture.Core.SearchEngine.Abstractions.Models;

/// <summary>
/// Represents the result of a search operation.
/// </summary>
/// <param name="Success">Indicates whether the operation was successful.</param>
/// <param name="Message">Provides details about the operation result.</param>
public readonly record struct SearchResult(bool Success, string Message);
