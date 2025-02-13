using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.EntityFramework.Paging;

public static class IQueryablePaginateExtensions
{
    private const int MaxPageSize = 100_000;

    private static void ValidatePaginationParameters(int index, int size)
    {
        if (index < 0)
            throw new ArgumentException("Page index cannot be negative.", nameof(index));
        if (index == int.MaxValue)
            throw new ArgumentException("Page index is too large.", nameof(index));
            
        if (size <= 0)
            throw new ArgumentException("Page size must be greater than 0.", nameof(size));
        if (size == int.MaxValue || size > MaxPageSize)
            throw new ArgumentException("Page size is too large.", nameof(size));

        if (index >= int.MaxValue / size)
            throw new ArgumentException("Page index and size combination would cause arithmetic overflow.", nameof(index));
    }

    public static async Task<IPaginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        int index,
        int size,
        CancellationToken cancellationToken = default
    )
    {
        ValidatePaginationParameters(index, size);

        int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await source.Skip(index * size).Take(size).ToListAsync(cancellationToken).ConfigureAwait(false);
        var pages = (int)Math.Ceiling(count / (double)size);

        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Pages = pages,
            Items = items
        };
        return list;
    }

    public static IPaginate<T> ToPaginate<T>(this IQueryable<T> source, int index, int size)
    {
        ValidatePaginationParameters(index, size);

        int count = source.Count();
        var items = source.Skip(index * size).Take(size).ToList();
        var pages = (int)Math.Ceiling(count / (double)size);

        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Pages = pages,
            Items = items
        };
        return list;
    }
}
