using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

public class NotFoundProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Resource not found";
    private const string DEFAULT_TYPE = "https://example.com/probs/notfound";

    public NotFoundProblemDetails(string detail, string title = DEFAULT_TITLE, string type = DEFAULT_TYPE)
    {
        Title = title;
        Detail = detail;
        Status = StatusCodes.Status404NotFound;
        Type = type;
    }
}
