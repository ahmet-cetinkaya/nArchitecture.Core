using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;
using NArchitecture.Core.Mediator.Abstractions.CQRS;

namespace NArchitecture.Core.Mediator;

/// <summary>
/// Extension methods for registering mediator services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Adds mediator services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IMediator, MediatorImpl>();

        RegisterHandlers(services, assemblies);
        RegisterBehaviors(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        // Register request handlers
        RegisterHandlersOfType(services, typeof(IRequestHandler<,>), assemblies);
        RegisterHandlersOfType(services, typeof(IRequestHandler<>), assemblies);

        // Register CQRS handlers
        RegisterHandlersOfType(services, typeof(IQueryHandler<,>), assemblies);
        RegisterHandlersOfType(services, typeof(ICommandHandler<,>), assemblies);
        RegisterHandlersOfType(services, typeof(ICommandHandler<>), assemblies);

        // Register event handlers
        RegisterHandlersOfType(services, typeof(IEventHandler<>), assemblies);
    }

    /// <summary>
    /// Registers all pipeline behaviors from the provided assemblies.
    /// </summary>
    private static void RegisterBehaviors(IServiceCollection services, Assembly[] assemblies)
    {
        // Register behaviors for requests with response
        RegisterBehaviorsOfType(services, typeof(IPipelineBehavior<,>), assemblies);

        // Register behaviors for requests without response
        RegisterBehaviorsOfType(services, typeof(IPipelineBehavior<>), assemblies);
    }

    private static void RegisterBehaviorsOfType(IServiceCollection services, Type behaviorType, Assembly[] assemblies)
    {
        var behaviorImplementations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && !type.IsNested
                && // Skip nested types
                type.IsPublic
                && // Only register public types
                type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == behaviorType)
            );

        foreach (var implementation in behaviorImplementations)
        {
            // Skip types that have constructor parameters which would prevent auto-registration
            if (
                implementation
                    .GetConstructors()
                    .All(ctor => ctor.GetParameters().Length > 0 && !ctor.GetParameters().All(p => p.HasDefaultValue))
            )
            {
                continue;
            }

            var implementedInterfaces = implementation
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == behaviorType);

            foreach (var implementedInterface in implementedInterfaces)
            {
                // Register the behavior as its open generic implementation
                if (implementation.IsGenericType)
                {
                    services.AddTransient(behaviorType, implementation.GetGenericTypeDefinition());
                }
                else
                {
                    services.AddTransient(implementedInterface, implementation);
                }
            }
        }
    }

    private static void RegisterHandlersOfType(IServiceCollection services, Type handlerType, Assembly[] assemblies)
    {
        var handlerImplementations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
            );

        foreach (var implementation in handlerImplementations)
        {
            var implementedInterfaces = implementation
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType);

            foreach (var implementedInterface in implementedInterfaces)
            {
                services.AddTransient(implementedInterface, implementation);

                // Also register the base interface for CQRS handlers
                if (handlerType == typeof(IQueryHandler<,>))
                {
                    var genericArgs = implementedInterface.GetGenericArguments();
                    var baseRequestHandlerType = typeof(IRequestHandler<,>).MakeGenericType(genericArgs);
                    services.AddTransient(baseRequestHandlerType, implementation);
                }
                else if (handlerType == typeof(ICommandHandler<,>))
                {
                    var genericArgs = implementedInterface.GetGenericArguments();
                    var baseRequestHandlerType = typeof(IRequestHandler<,>).MakeGenericType(genericArgs);
                    services.AddTransient(baseRequestHandlerType, implementation);
                }
                else if (handlerType == typeof(ICommandHandler<>))
                {
                    var genericArgs = implementedInterface.GetGenericArguments();
                    var baseRequestHandlerType = typeof(IRequestHandler<>).MakeGenericType(genericArgs);
                    services.AddTransient(baseRequestHandlerType, implementation);
                }
            }
        }
    }
}
