using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

public class InternalServerErrorProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Internal server error";
    private const string DEFAULT_TYPE = "https://example.com/probs/internal";
    private const string DEFAULT_DETAIL = "An internal server error occurred";

    public InternalServerErrorProblemDetails(
        string? detail = DEFAULT_DETAIL,
        string title = DEFAULT_TITLE,
        string type = DEFAULT_TYPE
    )
    {
        Title = title;
        Detail = string.IsNullOrEmpty(detail) ? DEFAULT_DETAIL : detail;
        Status = StatusCodes.Status500InternalServerError;
        Type = type;
    }
}
