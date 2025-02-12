using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.EntityFramework.Paging;

public static class IQueryablePaginateExtensions
{
    public static async Task<IPaginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        uint index,
        uint size,
        CancellationToken cancellationToken = default
    )
    {
        uint count = Convert.ToUInt32(await source.LongCountAsync(cancellationToken).ConfigureAwait(false));
        List<T> items = await source
            .Skip(Convert.ToInt32((index) * size))
            .Take(Convert.ToInt32(size))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Items = items,
            Pages = Convert.ToUInt32(Math.Ceiling(count / (double)size)),
        };
        return list;
    }

    public static IPaginate<T> ToPaginate<T>(this IQueryable<T> source, uint index, uint size)
    {
        uint count = Convert.ToUInt32(source.LongCount());
        var items = source.Skip(Convert.ToInt32(index * size)).Take(Convert.ToInt32(size)).ToList();

        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            Count = count,
            Items = items,
            Pages = Convert.ToUInt32(Math.Ceiling(count / (double)size)),
        };
        return list;
    }
}
