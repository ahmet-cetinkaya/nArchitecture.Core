using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;
using NArchitecture.Core.Mediator.Abstractions.CQRS;

namespace NArchitecture.Core.Mediator;

/// <summary>
/// Implementation of the <see cref="IMediator"/> interface.
/// </summary>
public class MediatorImpl : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MediatorImpl(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceProvider = serviceProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Type requestType = request.GetType();
        Type responseType = typeof(TResponse);

        return CreateRequestPipelineWithResponse<TResponse>(request, requestType, responseType, cancellationToken);
    }

    // Fix to use proper scope for resolving handlers
    private async Task<TResponse> CreateRequestPipelineWithResponse<TResponse>(
        IRequest<TResponse> request,
        Type requestType,
        Type responseType,
        CancellationToken cancellationToken
    )
    {
        // Create a scope to resolve handlers with scoped dependencies
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        object? handler = serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"Handler not found for {requestType.Name}");
        }

        // Get the handle method
        var handleMethod = handlerType.GetMethod("Handle")!;

        // Create the core request handler delegate
        RequestHandlerDelegate<TResponse> coreHandlerDelegate = async () =>
        {
            var result = await (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
            return result;
        };

        // Get the closed behavior type for this specific request/response
        var closedBehaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

        // Get behaviors directly registered for this specific request/response type
        var behaviors = serviceProvider.GetServices(closedBehaviorType).ToArray();

        // If no behaviors, just execute the handler directly
        if (behaviors.Length == 0)
            return await coreHandlerDelegate();

        // Build the pipeline by wrapping behaviors around the core handler
        var behaviorHandleMethod = closedBehaviorType.GetMethod("Handle")!;

        // Start with the core handler
        RequestHandlerDelegate<TResponse> pipeline = coreHandlerDelegate;

        // Wrap each behavior around the pipeline in reverse order of registration
        // This ensures first registered behavior executes first
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentPipeline = pipeline; // Capture the current pipeline

            // Create a new pipeline that wraps the current one with this behavior
            pipeline = async () =>
            {
                var result = await (Task<TResponse>)
                    behaviorHandleMethod.Invoke(behavior, new object[] { request, currentPipeline, cancellationToken })!;
                return result;
            };
        }

        // Execute the pipeline and return the result
        return await pipeline();
    }

    public Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        return CreateRequestPipeline(request, requestType, cancellationToken);
    }

    private async Task CreateRequestPipeline(IRequest request, Type requestType, CancellationToken cancellationToken)
    {
        // Create a scope to resolve handlers with scoped dependencies
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Get the handler type for this request
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        // Get the handler instance
        var handler =
            serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        // Get the handle method
        var handleMethod = handlerType.GetMethod("Handle")!;

        // Create the core request handler delegate
        RequestHandlerDelegate coreHandlerDelegate = async () =>
        {
            await (Task)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
        };

        // Get the closed behavior type for this specific request
        var closedBehaviorType = typeof(IPipelineBehavior<>).MakeGenericType(requestType);

        // Get behaviors directly registered for this specific request type
        var behaviors = serviceProvider.GetServices(closedBehaviorType).ToArray();

        // If no behaviors, just execute the handler directly
        if (behaviors.Length == 0)
        {
            await coreHandlerDelegate();
            return;
        }

        // Build the pipeline by wrapping behaviors around the core handler
        var behaviorHandleMethod = closedBehaviorType.GetMethod("Handle")!;

        // Start with the core handler
        RequestHandlerDelegate pipeline = coreHandlerDelegate;

        // Wrap each behavior around the pipeline in reverse order of registration
        // This ensures first registered behavior executes first
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentPipeline = pipeline; // Capture the current pipeline

            // Create a new pipeline that wraps the current one with this behavior
            pipeline = async () =>
            {
                await (Task)behaviorHandleMethod.Invoke(behavior, new object[] { request, currentPipeline, cancellationToken })!;
            };
        }

        // Execute the pipeline
        await pipeline();
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);

        // Get all handlers using GetService with IEnumerable<T> instead of GetServices
        var handlers = (_serviceProvider.GetService(enumerableType) as IEnumerable<object>) ?? Enumerable.Empty<object>();

        if (!handlers.Any())
            return;

        var method = handlerType.GetMethod("Handle")!;
        var tasks = handlers
            .Select(handler => (Task)method.Invoke(handler, new object[] { @event, cancellationToken })!)
            .ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // If any tasks faulted, collect all exceptions into an AggregateException
            var exceptions = tasks
                .Where(t => t.IsFaulted && t.Exception != null)
                .SelectMany(t => t.Exception!.InnerExceptions)
                .ToList();

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }

            throw; // Re-throw the original exception if we couldn't determine the cause
        }
    }
}
