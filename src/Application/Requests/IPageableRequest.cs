namespace NArchitecture.Core.Application.Requests;

public interface IPageableRequest
{
    int PageIndex { get; set; }
    int PageSize { get; set; }
}
