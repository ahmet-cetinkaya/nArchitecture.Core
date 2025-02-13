namespace NArchitecture.Core.Persistence.Abstractions.Paging;

public class Paginate<T> : IPaginate<T>
{
    public Paginate(IEnumerable<T> source, int index, int size)
    {
        Index = index;
        Size = size;

        if (source is IQueryable<T> queryable)
        {
            Count = queryable.Count();
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
            Items = queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size)).ToArray();
        }
        else
        {
            T[] enumerable = source as T[] ?? [.. source];
            Count = Convert.ToInt32(enumerable.Length);
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
            Items = enumerable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size)).ToArray();
        }
    }

    public Paginate()
    {
        Items = Array.Empty<T>();
    }

    public int Index { get; set; }
    public int Size { get; set; }
    public int Count { get; set; }
    public int Pages { get; set; }
    public ICollection<T> Items { get; set; }
    public bool HasPrevious => Index > 0;
    public bool HasNext => Index + 1 < Pages;
}

public class Paginate<TSource, TResult> : IPaginate<TResult>
{
    public Paginate(IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter, int index, int size)
    {
        Index = index;
        Size = size;

        if (source is IQueryable<TSource> queryable)
        {
            Count = queryable.Count();
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
            TSource[] items = [.. queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size))];
            Items = [.. converter(items)];
        }
        else
        {
            TSource[] enumerable = source as TSource[] ?? [.. source];
            Count = enumerable.Length;
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
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

    public int Index { get; }
    public int Size { get; }
    public int Count { get; }
    public int Pages { get; }
    public ICollection<TResult> Items { get; }
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
