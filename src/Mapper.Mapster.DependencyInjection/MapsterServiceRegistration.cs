using System.Reflection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mapper.Abstractions;

namespace NArchitecture.Core.Mapper.Mapster.DependencyInjection;

/// <summary>
/// Extension methods for registering Mapster services with dependency injection.
/// </summary>
public static class MapsterServiceRegistration
{
    /// <summary>
    /// Adds Mapster profiles from the specified assembly and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapster(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        return AddNArchitectureMapster(services, null, ServiceLifetime.Singleton, true, assembly);
    }

    /// <summary>
    /// Adds Mapster profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapster(this IServiceCollection services, params Assembly[] assemblies)
    {
        return AddNArchitectureMapster(services, null, ServiceLifetime.Singleton, true, assemblies);
    }

    /// <summary>
    /// Adds Mapster profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapster(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies
    )
    {
        return AddNArchitectureMapster(services, null, lifetime, true, assemblies);
    }

    /// <summary>
    /// Adds Mapster profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure Mapster.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapster(
        this IServiceCollection services,
        Action<TypeAdapterConfig>? configAction,
        params Assembly[] assemblies
    )
    {
        return AddNArchitectureMapster(services, configAction, ServiceLifetime.Singleton, true, assemblies);
    }

    /// <summary>
    /// Adds Mapster profiles from the specified assemblies and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure Mapster.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="filterByInterface">When true, only profiles implementing IMappingProfile will be used. When false, all IRegister implementations will be used.</param>
    /// <param name="assemblies">The assemblies containing mapping profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapster(
        this IServiceCollection services,
        Action<TypeAdapterConfig>? configAction,
        ServiceLifetime lifetime,
        bool filterByInterface = true,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        // Create a TypeAdapterConfig with profiles
        var config = new TypeAdapterConfig();

        // Apply any provided configuration
        configAction?.Invoke(config);

        if (filterByInterface)
        {
            // Find all profile types that implement IMappingProfile<T> and IRegister
            var profileTypes = assemblies
                .Where(a => a != null)
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    !t.IsInterface
                    && !t.IsAbstract
                    && typeof(IRegister).IsAssignableFrom(t)
                    && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMappingProfile<>))
                );

            // Register profiles marked with the interface
            foreach (var profileType in profileTypes)
            {
                if (Activator.CreateInstance(profileType) is IRegister profile)
                {
                    profile.Register(config);
                }
            }
        }
        else
        {
            // Use all IRegister implementations from the assemblies
            var registerTypes = assemblies
                .Where(a => a != null)
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IRegister).IsAssignableFrom(t));

            foreach (var registerType in registerTypes)
            {
                if (Activator.CreateInstance(registerType) is IRegister register)
                {
                    register.Register(config);
                }
            }
        }

        // Register TypeAdapterConfig with the specified lifetime
        services.Add(new ServiceDescriptor(typeof(TypeAdapterConfig), serviceProvider => config, lifetime));

        // Register our adapter with the specified lifetime
        services.Add(
            new ServiceDescriptor(
                typeof(NArchitecture.Core.Mapper.Abstractions.IMapper),
                serviceProvider => new MapsterAdapter(serviceProvider.GetRequiredService<TypeAdapterConfig>()),
                lifetime
            )
        );

        return services;
    }

    /// <summary>
    /// Adds Mapster with global configuration and registers the adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configAction">Action to configure the global TypeAdapterConfig.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNArchitectureMapsterGlobal(
        this IServiceCollection services,
        Action<TypeAdapterConfig>? configAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton
    )
    {
        // Configure global TypeAdapterConfig
        configAction?.Invoke(TypeAdapterConfig.GlobalSettings);

        // Register our adapter using the global configuration
        services.Add(
            new ServiceDescriptor(
                typeof(NArchitecture.Core.Mapper.Abstractions.IMapper),
                serviceProvider => new MapsterAdapter(),
                lifetime
            )
        );

        return services;
    }
}
