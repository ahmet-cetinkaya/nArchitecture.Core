namespace NArchitecture.Core.Persistence.Abstractions.Paging;

public interface IPaginate<T>
{
    uint Index { get; }
    uint Size { get; }
    uint Count { get; }
    uint Pages { get; }
    IEnumerable<T> Items { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}
