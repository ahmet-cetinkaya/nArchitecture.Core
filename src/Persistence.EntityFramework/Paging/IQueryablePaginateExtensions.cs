using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.EntityFramework.Paging;

/// <summary>
/// Provides extension methods for paginating queryable collections.
/// </summary>
public static class IQueryablePaginateExtensions
{
    private const int MaxPageSize = 100_000;

    private static void ValidatePaginationParameters(int index, int size)
    {
        if (index < 0)
            throw new ArgumentException(PaginationMessages.PageIndexCannotBeNegative, nameof(index));
        if (index == int.MaxValue)
            throw new ArgumentException(PaginationMessages.PageIndexTooLarge, nameof(index));
        if (size <= 0)
            throw new ArgumentException(PaginationMessages.PageSizeMustBeGreaterThanZero, nameof(size));
        if (size is int.MaxValue or > MaxPageSize)
            throw new ArgumentException(PaginationMessages.PageSizeTooLarge, nameof(size));
        if (index >= int.MaxValue / size)
            throw new ArgumentException(PaginationMessages.PageIndexSizeCombinationOverflow, nameof(index));
    }

    /// <summary>
    /// Asynchronously creates a paginated result for the specified query.
    /// </summary>
    /// <typeparam name="T">The type of the items in the source.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="index">The page index (zero-based).</param>
    /// <param name="size">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="IPaginate{T}"/> containing the paginated items, page count, and related metadata.
    /// </returns>
    public static async Task<IPaginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        int index,
        int size,
        CancellationToken cancellationToken = default
    )
    {
        ValidatePaginationParameters(index, size);

        int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        List<T> items = await source.Skip(index * size).Take(size).ToListAsync(cancellationToken).ConfigureAwait(false);
        int pages = (int)Math.Ceiling(count / (double)size);

        // Build the paginated result
        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Pages = pages,
            Items = items,
        };
        return list;
    }

    /// <summary>
    /// Creates a paginated result for the specified query.
    /// </summary>
    /// <typeparam name="T">The type of the items in the source.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="index">The page index (zero-based).</param>
    /// <param name="size">The number of items per page.</param>
    /// <returns>
    /// An <see cref="IPaginate{T}"/> containing the paginated items, page count, and related metadata.
    /// </returns>
    public static IPaginate<T> ToPaginate<T>(this IQueryable<T> source, int index, int size)
    {
        ValidatePaginationParameters(index, size);

        int count = source.Count();
        var items = source.Skip(index * size).Take(size).ToList();
        int pages = (int)Math.Ceiling(count / (double)size);

        // Build the paginated result
        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Pages = pages,
            Items = items,
        };
        return list;
    }
}

file static class PaginationMessages
{
    public const string PageIndexCannotBeNegative = "Page index cannot be negative.";
    public const string PageIndexTooLarge = "Page index is too large.";
    public const string PageSizeMustBeGreaterThanZero = "Page size must be greater than 0.";
    public const string PageSizeTooLarge = "Page size is too large.";
    public const string PageIndexSizeCombinationOverflow = "Page index and size combination would cause arithmetic overflow.";
}
