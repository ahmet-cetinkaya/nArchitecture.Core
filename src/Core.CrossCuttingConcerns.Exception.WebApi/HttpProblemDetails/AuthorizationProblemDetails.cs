using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

public class AuthorizationProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Authorization error";
    private const string DEFAULT_TYPE = "https://example.com/probs/authorization";

    public AuthorizationProblemDetails(string detail, string title = DEFAULT_TITLE, string type = DEFAULT_TYPE)
    {
        Detail = detail;
        Title = title;
        Status = StatusCodes.Status401Unauthorized;
        Type = type;
    }
}
