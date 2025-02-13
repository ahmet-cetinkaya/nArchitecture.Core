namespace NArchitecture.Core.Persistence.Abstractions.Paging;

public interface IPaginate<T>
{
    int Index { get; }
    int Size { get; }
    int Count { get; }
    int Pages { get; }
    ICollection<T> Items { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}
