using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Extensions;

/// <summary>
/// Provides extension methods for ProblemDetails serialization.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts a ProblemDetails object to its JSON representation.
    /// </summary>
    public static string ToJson<TProblemDetail>(this TProblemDetail details)
        where TProblemDetail : ProblemDetails
    {
        return JsonSerializer.Serialize(details);
    }
}
