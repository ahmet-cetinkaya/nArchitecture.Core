using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;

namespace NArchitecture.Core.Mediator;

/// <summary>
/// Implementation of the <see cref="IMediator"/> interface.
/// </summary>
internal sealed class MediatorImpl : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorImpl"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public MediatorImpl(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Create the pipeline for this request
        var pipeline = CreateRequestPipelineWithResponse(request, requestType, responseType, cancellationToken);

        // Execute the pipeline
        return await pipeline();
    }

    /// <inheritdoc/>
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        // Create the pipeline for this request
        var pipeline = CreateRequestPipeline(request, requestType, cancellationToken);

        // Execute the pipeline
        await pipeline();
    }

    private RequestHandlerDelegate<TResponse> CreateRequestPipelineWithResponse<TResponse>(
        IRequest<TResponse> request,
        Type requestType,
        Type responseType,
        CancellationToken cancellationToken
    )
    {
        // Get the handler type for this request
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        // Get the handler instance
        var handler =
            _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

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
        var behaviors = _serviceProvider.GetServices(closedBehaviorType).ToArray();

        // If no behaviors, just return the handler
        if (behaviors.Length == 0)
            return coreHandlerDelegate;

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

        return pipeline;
    }

    private RequestHandlerDelegate CreateRequestPipeline(IRequest request, Type requestType, CancellationToken cancellationToken)
    {
        // Get the handler type for this request
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        // Get the handler instance
        var handler =
            _serviceProvider.GetService(handlerType)
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
        var behaviors = _serviceProvider.GetServices(closedBehaviorType).ToArray();

        // If no behaviors, just return the handler
        if (behaviors.Length == 0)
            return coreHandlerDelegate;

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

        return pipeline;
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
