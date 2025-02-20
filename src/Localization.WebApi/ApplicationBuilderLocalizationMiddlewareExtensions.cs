using Microsoft.AspNetCore.Builder;

namespace NArchitecture.Core.Localization.WebApi;

/// <summary>
/// Extension methods for adding the LocalizationMiddleware to the application's request pipeline.
/// </summary>
public static class ApplicationBuilderLocalizationMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="LocalizationMiddleware"/> to the application's middleware pipeline.
    /// </summary>
    /// <param name="builder">The IApplicationBuilder instance.</param>
    /// <returns>The updated IApplicationBuilder.</returns>
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LocalizationMiddleware>();
    }
}
