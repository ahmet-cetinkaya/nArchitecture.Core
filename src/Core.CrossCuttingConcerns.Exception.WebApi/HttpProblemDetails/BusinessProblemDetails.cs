using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

public class BusinessProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Business rule violation";
    private const string DEFAULT_TYPE = "https://example.com/probs/business";

    public BusinessProblemDetails(string detail, string title = DEFAULT_TITLE, string type = DEFAULT_TYPE)
    {
        Title = title;
        Detail = detail;
        Status = StatusCodes.Status400BadRequest;
        Type = type;
    }
}
