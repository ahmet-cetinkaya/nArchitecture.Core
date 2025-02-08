using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

public class ValidationProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Validation error(s)";
    private const string DEFAULT_TYPE = "https://example.com/probs/validation";

    public IEnumerable<ValidationError> Errors { get; init; }

    public ValidationProblemDetails(IEnumerable<ValidationError> errors, string title = DEFAULT_TITLE, string type = DEFAULT_TYPE)
    {
        Title = title;
        Status = StatusCodes.Status400BadRequest;
        Type = type;
        Errors = errors;
    }
}
