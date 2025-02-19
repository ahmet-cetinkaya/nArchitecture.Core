using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Translation.Abstraction;

namespace NArchitecture.Core.Translation.AmazonTranslate.DependencyInjection;

/// <summary>
/// Extension methods for configuring Amazon Translate services in dependency injection.
/// </summary>
public static class ServiceCollectionAmazonTranslateLocalizationExtension
{
    /// <summary>
    /// Adds Amazon Translate services to the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration settings for Amazon Translate.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAmazonTranslation(
        this IServiceCollection services,
        AmazonTranslateConfiguration configuration
    )
    {
        _ = services.AddTransient<ITranslationService, AmazonTranslateLocalizationManager>(
            _ => new AmazonTranslateLocalizationManager(configuration)
        );
        return services;
    }
}
