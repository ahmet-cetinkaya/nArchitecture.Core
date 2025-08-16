using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Application.Rules;

namespace NArchitecture.Core.Application.DependencyInjection;

/// <summary>
/// Extension methods for registering business rules with the dependency injection container.
/// </summary>
public static class ServiceCollectionBusinessRulesExtensions
{
    /// <summary>
    /// Registers all classes implementing <see cref="IBusinessRules"/> from the calling assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBusinessRules(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        return services.AddBusinessRules(lifetime, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Registers all classes implementing <see cref="IBusinessRules"/> from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for business rules.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBusinessRules(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddBusinessRules(ServiceLifetime.Scoped, assemblies);
    }

    /// <summary>
    /// Registers all classes implementing <see cref="IBusinessRules"/> from the specified assemblies with the specified lifetime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies to scan for business rules.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBusinessRules(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies
    )
    {
        foreach (Assembly assembly in assemblies)
        {
            var businessRuleTypes = assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IBusinessRules)))
                .ToList();

            foreach (var businessRuleType in businessRuleTypes)
            {
                services.Add(
                    new ServiceDescriptor(serviceType: businessRuleType, implementationType: businessRuleType, lifetime: lifetime)
                );
            }
        }

        return services;
    }
}
