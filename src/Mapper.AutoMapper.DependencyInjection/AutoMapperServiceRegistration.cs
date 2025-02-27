using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mapper.Abstractions;

namespace NArchitecture.Core.Mapper.AutoMapper.DependencyInjection;

/// <summary>
/// Extension methods for registering AutoMapper services with dependency injection.
/// </summary>
public static class AutoMapperServiceRegistration
{
    /// <summary>
    /// Adds AutoMapper profiles that implement IMappingProfile and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddAutoMapper(services, null, ServiceLifetime.Singleton, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles that implement IMappingProfile and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies
    )
    {
        return AddAutoMapper(services, null, lifetime, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles that implement IMappingProfile and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure AutoMapper.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(
        this IServiceCollection services,
        Action<IMapperConfigurationExpression>? configAction,
        params Assembly[] assemblies
    )
    {
        return AddAutoMapper(services, configAction, ServiceLifetime.Singleton, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles that implement IMappingProfile and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure AutoMapper.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(
        this IServiceCollection services,
        Action<IMapperConfigurationExpression>? configAction,
        ServiceLifetime lifetime,
        params Assembly[] assemblies
    )
    {
        // Find all profile types marked with IMappingProfile
        var profileTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IMappingProfile).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        // Create a configuration with those profiles
        Action<IMapperConfigurationExpression> configurationAction = cfg =>
        {
            // Add any provided configuration
            configAction?.Invoke(cfg);

            // Add profiles marked with the interface
            foreach (var profileType in profileTypes)
            {
                cfg.AddProfile(profileType);
            }
        };

        // Register AutoMapper
        services.AddAutoMapper(configurationAction);

        // Register our adapter with the specified lifetime
        services.Add(
            new ServiceDescriptor(
                typeof(NArchitecture.Core.Mapper.Abstractions.IMapper),
                serviceProvider => new AutoMapperAdapter(serviceProvider.GetRequiredService<global::AutoMapper.IMapper>()),
                lifetime
            )
        );

        return services;
    }

    /// <summary>
    /// Adds all AutoMapper profiles from the assemblies without filtering by IMappingProfile.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAllAutoMapperProfiles(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddAllAutoMapperProfiles(services, null, ServiceLifetime.Singleton, assemblies);
    }

    /// <summary>
    /// Adds all AutoMapper profiles from the assemblies without filtering by IMappingProfile.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure AutoMapper.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAllAutoMapperProfiles(
        this IServiceCollection services,
        Action<IMapperConfigurationExpression>? configAction,
        ServiceLifetime lifetime,
        params Assembly[] assemblies
    )
    {
        // Register AutoMapper
        services.AddAutoMapper(assemblies, configAction);

        // Register our adapter with the specified lifetime
        services.Add(
            new ServiceDescriptor(
                typeof(NArchitecture.Core.Mapper.Abstractions.IMapper),
                serviceProvider => new AutoMapperAdapter(serviceProvider.GetRequiredService<global::AutoMapper.IMapper>()),
                lifetime
            )
        );

        return services;
    }
}
