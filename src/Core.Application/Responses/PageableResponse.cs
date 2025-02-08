using NArchitecture.Core.Persistence.Paging;

namespace NArchitecture.Core.Application.Responses;

public class PageableResponse<T> : BasePageableModel
{
    private IEnumerable<T>? _items;

    public IEnumerable<T> Items
    {
        get => _items ??= Array.Empty<T>();
        set => _items = value;
    }
}
