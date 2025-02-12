namespace NArchitecture.Core.Persistence.Abstractions.Paging;

public class Paginate<T> : IPaginate<T>
{
    public Paginate(IEnumerable<T> source, uint index, uint size)
    {
        Index = index;
        Size = size;

        if (source is IQueryable<T> queryable)
        {
            Count = Convert.ToUInt32(queryable.LongCount());
            Pages = Convert.ToUInt32(Math.Ceiling(Count / (double)Size));
            Items = queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size)).ToArray();
        }
        else
        {
            T[] enumerable = source as T[] ?? [.. source];
            Count = Convert.ToUInt32(enumerable.LongCount());
            Pages = Convert.ToUInt32(Math.Ceiling(Count / (double)Size));
            Items = enumerable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size)).ToArray();
        }
    }

    public Paginate()
    {
        Items = Array.Empty<T>();
    }

    public uint Index { get; set; }
    public uint Size { get; set; }
    public uint Count { get; set; }
    public uint Pages { get; set; }
    public IEnumerable<T> Items { get; set; }
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index + 1 < Pages;
}

public class Paginate<TSource, TResult> : IPaginate<TResult>
{
    public Paginate(
        IEnumerable<TSource> source,
        Func<IEnumerable<TSource>, IEnumerable<TResult>> converter,
        uint index,
        uint size
    )
    {
        Index = index;
        Size = size;

        if (source is IQueryable<TSource> queryable)
        {
            Count = Convert.ToUInt32(queryable.LongCount());
            Pages = Convert.ToUInt32(Math.Ceiling(Count / (double)Size));
            TSource[] items = [.. queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size))];
            Items = [.. converter(items)];
        }
        else
        {
            TSource[] enumerable = source as TSource[] ?? [.. source];
            Count = Convert.ToUInt32(enumerable.LongCount());
            Pages = Convert.ToUInt32(Math.Ceiling(Count / (double)Size));
            TSource[] items = [.. enumerable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size))];
            Items = [.. converter(items)];
        }
    }

    public Paginate(IPaginate<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter)
    {
        Index = source.Index;
        Size = source.Size;
        Count = source.Count;
        Pages = source.Pages;
        Items = [.. converter(source.Items)];
    }

    public uint Index { get; }
    public uint Size { get; }
    public uint Count { get; }
    public uint Pages { get; }
    public IEnumerable<TResult> Items { get; }
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index + 1 < Pages;
}

public static class Paginate
{
    public static IPaginate<T> Empty<T>()
    {
        return new Paginate<T>();
    }

    public static IPaginate<TResult> From<TResult, TSource>(
        IPaginate<TSource> source,
        Func<IEnumerable<TSource>, IEnumerable<TResult>> converter
    )
    {
        return new Paginate<TSource, TResult>(source, converter);
    }
}
