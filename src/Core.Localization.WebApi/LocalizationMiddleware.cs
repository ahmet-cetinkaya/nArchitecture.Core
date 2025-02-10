using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.Localization.Abstraction;

namespace NArchitecture.Core.Localization.WebApi;

/// <summary>
/// Middleware that extracts the Accept-Language header and sets the AcceptLocales property of the provided ILocalizationService.
/// </summary>
public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Extracts accepted languages from the HTTP request headers and assigns them to the localization service.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="localizationService">The localization service instance.</param>
    public async Task Invoke(HttpContext context, ILocalizationService localizationService)
    {
        var headers = context.Request.GetTypedHeaders();
        // Ensure headers and AcceptLanguage are available before processing
        if (headers?.AcceptLanguage != null && headers.AcceptLanguage.Count > 0)
        {
            localizationService.AcceptLocales = headers
                .AcceptLanguage.OrderByDescending(x => x.Quality ?? 1)
                .Select(x => x.Value.ToString())
                .ToImmutableArray();
        }

        await _next(context);
    }
}
