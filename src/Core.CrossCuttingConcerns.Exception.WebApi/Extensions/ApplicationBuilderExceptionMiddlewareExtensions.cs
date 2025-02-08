using Microsoft.AspNetCore.Builder;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Middleware;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Extensions;

/// <summary>
/// Provides extension methods for configuring exception handling middleware.
/// </summary>
public static class ApplicationBuilderExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the custom exception handling middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder instance.</param>
    public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
    {
        _ = app.UseMiddleware<ExceptionMiddleware>();
    }
}
