using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

namespace NArchitecture.Core.CrossCuttingConcerns.Logging.DependencyInjection;

/// <summary>
/// Provides extension methods for IServiceCollection to configure logging services.
/// </summary>
public static class ServiceCollectionLoggingExtensions
{
    /// <summary>
    /// Adds logging services to the specified IServiceCollection with the provided logger implementation.
    /// </summary>
    /// <param name="logger">The logger implementation to be registered.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddLogging(this IServiceCollection services, ILogger logger)
    {
        _ = services.AddSingleton(logger);

        return services;
    }
}
