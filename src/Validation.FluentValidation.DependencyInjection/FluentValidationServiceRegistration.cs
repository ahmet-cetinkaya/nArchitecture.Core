using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Validation.Abstractions;

namespace NArchitecture.Core.Validation.FluentValidation.DependencyInjection;

/// <summary>
/// Extension methods for registering FluentValidation services with dependency injection.
/// </summary>
public static class FluentValidationServiceRegistration
{
    /// <summary>
    /// Adds FluentValidation validators from IValidationProfile implementations and registers adapters.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <param name="lifetime">The service lifetime (defaults to Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluentValidation(
        this IServiceCollection services,
        Assembly[] assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        // Find all validator types marked with IValidationProfile<T> in the assemblies
        var profileTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationProfile<>))
                && !t.IsInterface
                && !t.IsAbstract
            );

        // Find assemblies containing marked validators
        var assembliesToScan = profileTypes.Select(t => t.Assembly).Distinct().ToArray();

        if (assembliesToScan.Length > 0)
        {
            // Register all validators from these assemblies
            services.AddValidatorsFromAssemblies(assembliesToScan, lifetime);

            // Find validator types in these assemblies
            var validatorTypes = assembliesToScan
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.GetInterfaces()
                        .Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(global::FluentValidation.IValidator<>)
                        )
                    && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationProfile<>))
                );

            // Register our adapters for each validator type
            foreach (var validatorType in validatorTypes)
            {
                var entityType = validatorType
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(global::FluentValidation.IValidator<>))
                    .GetGenericArguments()[0];

                // Create generic FluentValidatorAdapter type for this entity
                var adapterType = typeof(FluentValidatorAdapter<>).MakeGenericType(entityType);
                var validatorServiceType = typeof(NArchitecture.Core.Validation.Abstractions.IValidator<>).MakeGenericType(
                    entityType
                );
                var fluentValidatorServiceType = typeof(global::FluentValidation.IValidator<>).MakeGenericType(entityType);

                // Register the adapter
                services.Add(
                    new ServiceDescriptor(
                        validatorServiceType,
                        serviceProvider =>
                            Activator.CreateInstance(
                                adapterType,
                                serviceProvider.GetRequiredService(fluentValidatorServiceType)
                            )!,
                        lifetime
                    )
                );
            }
        }

        return services;
    }

    /// <summary>
    /// Adds a FluentValidatorAdapter for the specified validator to the service collection.
    /// </summary>
    /// <typeparam name="T">The type of object to be validated.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (defaults to Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluentValidator<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where T : class
    {
        // Register FluentValidation's validator
        services.Add(
            new ServiceDescriptor(
                typeof(global::FluentValidation.IValidator<T>),
                serviceProvider => serviceProvider.GetRequiredService<global::FluentValidation.IValidator<T>>(),
                lifetime
            )
        );

        // Register our adapter
        services.Add(
            new ServiceDescriptor(
                typeof(NArchitecture.Core.Validation.Abstractions.IValidator<T>),
                serviceProvider => new FluentValidatorAdapter<T>(
                    serviceProvider.GetRequiredService<global::FluentValidation.IValidator<T>>()
                ),
                lifetime
            )
        );

        return services;
    }
}
