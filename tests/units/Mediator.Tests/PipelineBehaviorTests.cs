using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;
using NArchitecture.Core.Mediator.Abstractions.CQRS;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NArchitecture.Core.Mediator.Tests;

// Test request and handlers
file record TrackingRequest : IRequest<string>;

file class TrackingRequestHandler : IRequestHandler<TrackingRequest, string>
{
    private readonly ExecutionTracker _tracker;

    public TrackingRequestHandler(ExecutionTracker tracker) => _tracker = tracker;

    public Task<string> Handle(TrackingRequest request, CancellationToken cancellationToken)
    {
        _tracker.AddExecution("Handler");
        return Task.FromResult("Handler Result");
    }
}

// Request without response
file record TrackingRequestWithoutResponse : IRequest;

file class TrackingRequestWithoutResponseHandler : IRequestHandler<TrackingRequestWithoutResponse>
{
    private readonly ExecutionTracker _tracker;

    public TrackingRequestWithoutResponseHandler(ExecutionTracker tracker) => _tracker = tracker;

    public Task Handle(TrackingRequestWithoutResponse request, CancellationToken cancellationToken)
    {
        _tracker.AddExecution("Handler");
        return Task.CompletedTask;
    }
}

// Test behaviors
public class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ExecutionTracker _tracker;

    public FirstBehavior(ExecutionTracker tracker) => _tracker = tracker;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _tracker.AddExecution("FirstBehavior-Pre");
        var result = await next();
        _tracker.AddExecution("FirstBehavior-Post");
        return result;
    }
}

public class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ExecutionTracker _tracker;

    public SecondBehavior(ExecutionTracker tracker) => _tracker = tracker;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _tracker.AddExecution("SecondBehavior-Pre");
        var result = await next();
        _tracker.AddExecution("SecondBehavior-Post");
        return result;
    }
}

public class FirstBehaviorWithoutResponse<TRequest> : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    private readonly ExecutionTracker _tracker;

    public FirstBehaviorWithoutResponse(ExecutionTracker tracker) => _tracker = tracker;

    public async Task Handle(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        _tracker.AddExecution("FirstBehavior-Pre");
        await next();
        _tracker.AddExecution("FirstBehavior-Post");
    }
}

public class SecondBehaviorWithoutResponse<TRequest> : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    private readonly ExecutionTracker _tracker;

    public SecondBehaviorWithoutResponse(ExecutionTracker tracker) => _tracker = tracker;

    public async Task Handle(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        _tracker.AddExecution("SecondBehavior-Pre");
        await next();
        _tracker.AddExecution("SecondBehavior-Post");
    }
}

// Add a simple behavior without dependencies for auto-registration testing
public class AutoRegistrableBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        return next();
    }
}

// Helper for tracking execution order
public class ExecutionTracker
{
    public List<string> ExecutionOrder { get; } = new();
    
    public void AddExecution(string step) => ExecutionOrder.Add(step);
}

public class PipelineBehaviorTests
{
    [Fact(DisplayName = "Pipeline behaviors should execute in expected order for requests with response")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WithPipelineBehaviors_ShouldExecuteInExpectedOrder()
    {
        // Arrange
        var executionTracker = new ExecutionTracker();
        var services = new ServiceCollection();
        
        // Register mediator and handlers
        services.AddSingleton(executionTracker);
        services.AddSingleton<IMediator, MediatorImpl>();
        services.AddTransient<IRequestHandler<TrackingRequest, string>, TrackingRequestHandler>();
        
        // Register behaviors in order
        services.AddTransient<IPipelineBehavior<TrackingRequest, string>, FirstBehavior<TrackingRequest, string>>();
        services.AddTransient<IPipelineBehavior<TrackingRequest, string>, SecondBehavior<TrackingRequest, string>>();
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.SendAsync(new TrackingRequest(), CancellationToken.None);
        
        // Assert
        result.ShouldBe("Handler Result");
        executionTracker.ExecutionOrder.Count.ShouldBe(5);
        executionTracker.ExecutionOrder[0].ShouldBe("FirstBehavior-Pre");
        executionTracker.ExecutionOrder[1].ShouldBe("SecondBehavior-Pre");
        executionTracker.ExecutionOrder[2].ShouldBe("Handler");
        executionTracker.ExecutionOrder[3].ShouldBe("SecondBehavior-Post");
        executionTracker.ExecutionOrder[4].ShouldBe("FirstBehavior-Post");
    }
    
    [Fact(DisplayName = "Pipeline behaviors should execute in expected order for requests without response")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WithoutResponse_WithPipelineBehaviors_ShouldExecuteInExpectedOrder()
    {
        // Arrange
        var executionTracker = new ExecutionTracker();
        var services = new ServiceCollection();
        
        // Register mediator and handlers
        services.AddSingleton(executionTracker);
        services.AddSingleton<IMediator, MediatorImpl>();
        services.AddTransient<IRequestHandler<TrackingRequestWithoutResponse>, TrackingRequestWithoutResponseHandler>();
        
        // Register behaviors in order
        services.AddTransient<IPipelineBehavior<TrackingRequestWithoutResponse>, FirstBehaviorWithoutResponse<TrackingRequestWithoutResponse>>();
        services.AddTransient<IPipelineBehavior<TrackingRequestWithoutResponse>, SecondBehaviorWithoutResponse<TrackingRequestWithoutResponse>>();
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        await mediator.SendAsync(new TrackingRequestWithoutResponse(), CancellationToken.None);
        
        // Assert
        executionTracker.ExecutionOrder.Count.ShouldBe(5);
        executionTracker.ExecutionOrder[0].ShouldBe("FirstBehavior-Pre");
        executionTracker.ExecutionOrder[1].ShouldBe("SecondBehavior-Pre");
        executionTracker.ExecutionOrder[2].ShouldBe("Handler");
        executionTracker.ExecutionOrder[3].ShouldBe("SecondBehavior-Post");
        executionTracker.ExecutionOrder[4].ShouldBe("FirstBehavior-Post");
    }

    [Fact(DisplayName = "Request should be handled correctly with no pipeline behaviors")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WithNoBehaviors_ShouldHandleRequest()
    {
        // Arrange
        var executionTracker = new ExecutionTracker();
        var services = new ServiceCollection();
        
        // Register mediator and handlers without behaviors
        services.AddSingleton(executionTracker);
        services.AddSingleton<IMediator, MediatorImpl>();
        services.AddTransient<IRequestHandler<TrackingRequest, string>, TrackingRequestHandler>();
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act
        var result = await mediator.SendAsync(new TrackingRequest(), CancellationToken.None);
        
        // Assert
        result.ShouldBe("Handler Result");
        executionTracker.ExecutionOrder.Count.ShouldBe(1);
        executionTracker.ExecutionOrder[0].ShouldBe("Handler");
    }
    
    [Fact(DisplayName = "Pipeline behaviors should be correctly registered from assemblies")]
    [Trait("Category", "Unit")]
    public async Task AddMediator_ShouldRegisterPipelineBehaviorsFromAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add mediator with the current assembly (which contains our behaviors)
        services.AddMediator(typeof(PipelineBehaviorTests).Assembly);
        
        // Register handlers and required services manually
        services.AddTransient<ExecutionTracker>();
        services.AddTransient<IRequestHandler<TrackingRequest, string>, TrackingRequestHandler>();
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act - Get the auto-registered behavior
        var behavior = provider.GetService<IPipelineBehavior<TrackingRequest, string>>();
        
        // Assert
        behavior.ShouldNotBeNull();
        // TestRequestBehavior is being registered from ServiceRegistrationTests.cs 
        // and detected during assembly scanning
        behavior.ShouldBeOfType<TestRequestBehavior<TrackingRequest, string>>();
        
        // Test it works
        var result = await mediator.SendAsync(new TrackingRequest(), CancellationToken.None);
        result.ShouldBe("Handler Result");
    }
    
    [Fact(DisplayName = "Request handling should fail correctly with no handler registered")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WithNoHandlerRegistered_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, MediatorImpl>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await mediator.SendAsync(new TrackingRequest()));
            
        exception.Message.ShouldContain("No handler registered");
    }
}
