namespace NArchitecture.Core.Persistence.Abstractions.Paging;

/// <summary>
/// Provides pagination for a collection of items of type <typeparamref name="T"/>.
/// </summary>
public class Paginate<T> : IPaginate<T>
{
    public Paginate(IEnumerable<T> source, int index, int size)
    {
        Index = index;
        Size = size;

        if (source is IQueryable<T> queryable)
        {
            // If the collection supports IQueryable, let the database handle pagination.
            Count = queryable.Count();
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
            Items = queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size)).ToArray();
        }
        else
        {
            // Fallback to in-memory pagination.
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

    /// <inheritdoc/>
    public int Index { get; set; }

    /// <inheritdoc/>
    public int Size { get; set; }

    /// <inheritdoc/>
    public int Count { get; set; }

    /// <inheritdoc/>
    public int Pages { get; set; }

    /// <inheritdoc/>
    public ICollection<T> Items { get; set; }

    /// <inheritdoc/>
    public bool HasPrevious => Index > 0;

    /// <inheritdoc/>
    public bool HasNext => Index + 1 < Pages;
}

/// <summary>
/// Provides pagination allowing transformation from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.
/// </summary>
public class Paginate<TSource, TResult> : IPaginate<TResult>
{
    public Paginate(IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter, int index, int size)
    {
        Index = index;
        Size = size;

        if (source is IQueryable<TSource> queryable)
        {
            // Use IQueryable for efficient pagination when possible.
            Count = queryable.Count();
            Pages = Convert.ToInt32(Math.Ceiling(Count / (double)Size));
            TSource[] items = [.. queryable.Skip(Convert.ToInt32(Index * Size)).Take(Convert.ToInt32(Size))];
            Items = [.. converter(items)];
        }
        else
        {
            // Fallback to in-memory pagination.
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

    /// <inheritdoc/>
    public int Index { get; }

    /// <inheritdoc/>
    public int Size { get; }

    /// <inheritdoc/>
    public int Count { get; }

    /// <inheritdoc/>
    public int Pages { get; }

    /// <inheritdoc/>
    public ICollection<TResult> Items { get; }

    /// <inheritdoc/>
    public bool HasPrevious => Index > 0;

    /// <inheritdoc/>
    public bool HasNext => Index + 1 < Pages;
}

/// <summary>
/// Provides helper methods for pagination.
/// </summary>
public static class Paginate
{
    /// <summary>
    /// Returns an empty pagination result for type <typeparamref name="T"/>.
    /// </summary>
    public static IPaginate<T> Empty<T>()
    {
        return new Paginate<T>();
    }

    /// <summary>
    /// Creates a paginated result by applying a conversion function.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting items.</typeparam>
    /// <typeparam name="TSource">The type of the source items.</typeparam>
    /// <param name="source">The source paginated result.</param>
    /// <param name="converter">A function to convert source items to result items.</param>
    /// <returns>A paginated result of type <typeparamref name="TResult"/>.</returns>
    public static IPaginate<TResult> From<TResult, TSource>(
        IPaginate<TSource> source,
        Func<IEnumerable<TSource>, IEnumerable<TResult>> converter
    )
    {
        return new Paginate<TSource, TResult>(source, converter);
    }
}
