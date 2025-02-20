namespace NArchitecture.Core.Application.Requests;

public interface IPageableRequest
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
