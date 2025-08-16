using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails;

/// <summary>
/// Represents HTTP problem details for validation errors.
/// </summary>
public class ValidationProblemDetails : ProblemDetails
{
    private const string DEFAULT_TITLE = "Validation error(s)";
    private const string DEFAULT_TYPE = "https://example.com/probs/validation";

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IEnumerable<ValidationError> Errors { get; init; }

    /// <summary>
    /// Initializes a new instance of ValidationProblemDetails with specified validation errors.
    /// </summary>
    /// <param name="errors">Collection of validation errors.</param>
    /// <param name="title">Title of the error response.</param>
    /// <param name="type">Type of the error.</param>
    public ValidationProblemDetails(IEnumerable<ValidationError> errors, string title = DEFAULT_TITLE, string type = DEFAULT_TYPE)
    {
        Title = title;
        Status = StatusCodes.Status400BadRequest;
        Type = type;
        Errors = errors;
    }
}
