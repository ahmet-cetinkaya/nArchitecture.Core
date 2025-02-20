namespace NArchitecture.Core.Persistence.Abstractions.Paging;

/// <summary>
/// Represents a paginated result set.
/// </summary>
public interface IPaginate<T>
{
    /// <summary>
    /// Gets the current page index.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the size of each page.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets the total count of items.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    int Pages { get; }

    /// <summary>
    /// Gets the collection of items on the current page.
    /// </summary>
    ICollection<T> Items { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    bool HasPrevious { get; }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    bool HasNext { get; }
}
