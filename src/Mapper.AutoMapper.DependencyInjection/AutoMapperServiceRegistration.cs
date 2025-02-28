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
    /// Adds AutoMapper profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddAutoMapper(services, null, ServiceLifetime.Singleton, true, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles from the specified assemblies and registers the adapter.
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
        return AddAutoMapper(services, null, lifetime, true, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles from the specified assemblies and registers the adapter.
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
        return AddAutoMapper(services, configAction, ServiceLifetime.Singleton, true, assemblies);
    }

    /// <summary>
    /// Adds AutoMapper profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure AutoMapper.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="filterByInterface">When true, only profiles implementing IMappingProfile will be used. When false, all profiles will be used.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapper(
        this IServiceCollection services,
        Action<IMapperConfigurationExpression>? configAction,
        ServiceLifetime lifetime,
        bool filterByInterface = true,
        params Assembly[] assemblies
    )
    {
        // Create a configuration with profiles
        Action<IMapperConfigurationExpression> configurationAction = cfg =>
        {
            // Add any provided configuration
            configAction?.Invoke(cfg);

            if (filterByInterface)
            {
                // Find all profile types that implement IMappingProfile<T>
                var profileTypes = assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t =>
                        !t.IsInterface
                        && !t.IsAbstract
                        && t.GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMappingProfile<>))
                    );

                // Add profiles marked with the interface
                foreach (var profileType in profileTypes)
                {
                    cfg.AddProfile(profileType);
                }
            }
            else
            {
                // Use all profiles from the assemblies (AutoMapper's built-in logic will handle this)
                cfg.AddMaps(assemblies);
            }
        };

        // Register AutoMapper
        _ = services.AddAutoMapper(configurationAction);

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
