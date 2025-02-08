using Microsoft.AspNetCore.Builder;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Middleware;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Extensions;

public static class ApplicationBuilderExceptionMiddlewareExtensions
{
    public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
    {
        _ = app.UseMiddleware<ExceptionMiddleware>();
    }
}
