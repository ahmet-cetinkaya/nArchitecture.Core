using Microsoft.Extensions.DependencyInjection;
using Moq;
using NArchitecture.Core.Mediator.Abstractions;
using System.Collections.Concurrent;

namespace NArchitecture.Core.Mediator.Tests;

// Test requests and responses
file record TestRequestWithResponse(string Parameter) : IRequest<string>;
file record TestRequest(string Parameter) : IRequest;

// Test handlers
file class TestRequestWithResponseHandler : IRequestHandler<TestRequestWithResponse, string>
{
    public Task<string> Handle(TestRequestWithResponse request, CancellationToken cancellationToken)
        => Task.FromResult($"Handled: {request.Parameter}");
}

file class TestRequestHandler : IRequestHandler<TestRequest>
{
    private readonly List<string> _executionLog;

    public TestRequestHandler(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public Task Handle(TestRequest request, CancellationToken cancellationToken)
    {
        _executionLog.Add($"Handler executed for: {request.Parameter}");
        return Task.CompletedTask;
    }
}

// Test behaviors with execution tracking
file class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _executionLog;
    private readonly string _behaviorName;

    public LoggingBehavior(List<string> executionLog, string behaviorName)
    {
        _executionLog = executionLog;
        _behaviorName = behaviorName;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _executionLog.Add($"{_behaviorName} before");
        var response = await next();
        _executionLog.Add($"{_behaviorName} after");
        return response;
    }
}

file class LoggingBehavior<TRequest> : IPipelineBehavior<TRequest> 
    where TRequest : IRequest
{
    private readonly List<string> _executionLog;
    private readonly string _behaviorName;

    public LoggingBehavior(List<string> executionLog, string behaviorName)
    {
        _executionLog = executionLog;
        _behaviorName = behaviorName;
    }

    public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        _executionLog.Add($"{_behaviorName} before");
        await next();
        _executionLog.Add($"{_behaviorName} after");
    }
}

// Behavior that modifies request
file class RequestModifierBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is TestRequestWithResponse testRequest)
        {
            // Create a new request with modified parameter
            var modifiedRequest = new TestRequestWithResponse($"Modified-{testRequest.Parameter}");
            
            // Replace the request in the pipeline - handle null reference warning
            var parameterProp = typeof(TRequest).GetProperty("Parameter");
            if (parameterProp != null)
            {
                parameterProp.SetValue(request, modifiedRequest.Parameter);
            }
        }
        
        return await next();
    }
}

// Behavior that modifies response
file class ResponseModifierBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        
        if (response is string stringResponse)
        {
            // Convert using proper type checking to avoid null reference warning
            string modifiedResponse = $"Modified-{stringResponse}";
            return (TResponse)(object)modifiedResponse;
        }
        
        return response;
    }
}

// Short-circuit behaviors
file class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _executionLog;

    public ShortCircuitBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _executionLog.Add("Short-circuit behavior executed");
        
        // Don't call next() to short-circuit the pipeline
        return Task.FromResult((TResponse)(object)"Short-circuited");
    }
}

file class ShortCircuitBehavior<TRequest> : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    private readonly List<string> _executionLog;

    public ShortCircuitBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        _executionLog.Add("Short-circuit behavior executed");
        
        // Don't call next() to short-circuit the pipeline
        return Task.CompletedTask;
    }
}

public class MediatorImplTests
{
    [Fact(DisplayName = "SendAsync should execute pipeline behaviors in correct order")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldExecutePipelineBehaviorsInCorrectOrder()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        
        services.AddSingleton(executionLog);
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(
            sp => new LoggingBehavior<TestRequestWithResponse, string>(executionLog, "Behavior1"));
            
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(
            sp => new LoggingBehavior<TestRequestWithResponse, string>(executionLog, "Behavior2"));
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");
        
        // Act
        await mediator.SendAsync(request);
        
        // Assert
        executionLog.Count.ShouldBe(4);
        executionLog[0].ShouldBe("Behavior1 before");
        executionLog[1].ShouldBe("Behavior2 before");
        executionLog[2].ShouldBe("Behavior2 after");
        executionLog[3].ShouldBe("Behavior1 after");
    }
    
    [Fact(DisplayName = "SendAsync with void return should execute pipeline behaviors in correct order")]
    [Trait("Category", "Unit")]
    public async Task SendAsyncVoid_ShouldExecutePipelineBehaviorsInCorrectOrder()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        
        services.AddSingleton(executionLog);
        services.AddTransient<IRequestHandler<TestRequest>>(
            sp => new TestRequestHandler(executionLog));
        
        services.AddTransient<IPipelineBehavior<TestRequest>>(
            sp => new LoggingBehavior<TestRequest>(executionLog, "Behavior1"));
            
        services.AddTransient<IPipelineBehavior<TestRequest>>(
            sp => new LoggingBehavior<TestRequest>(executionLog, "Behavior2"));
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequest("Test");
        
        // Act
        await mediator.SendAsync(request);
        
        // Assert
        executionLog.Count.ShouldBe(5); // 4 behavior logs + 1 handler log
        executionLog[0].ShouldBe("Behavior1 before");
        executionLog[1].ShouldBe("Behavior2 before");
        executionLog[2].ShouldBe("Handler executed for: Test");
        executionLog[3].ShouldBe("Behavior2 after");
        executionLog[4].ShouldBe("Behavior1 after");
    }
    
    [Fact(DisplayName = "SendAsync should allow behaviors to modify the request")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldAllowBehaviorsToModifyRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        // Add request modifier behavior first
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>, RequestModifierBehavior<TestRequestWithResponse, string>>();
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Original");
        
        // Act
        var result = await mediator.SendAsync(request);
        
        // Assert
        result.ShouldBe("Handled: Modified-Original");
    }
    
    [Fact(DisplayName = "SendAsync should allow behaviors to modify the response")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldAllowBehaviorsToModifyResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        // Add response modifier behavior
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>, ResponseModifierBehavior<TestRequestWithResponse, string>>();
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");
        
        // Act
        var result = await mediator.SendAsync(request);
        
        // Assert
        result.ShouldBe("Modified-Handled: Test");
    }
    
    [Fact(DisplayName = "SendAsync should allow behaviors to short-circuit the pipeline")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldAllowBehaviorsToShortCircuitPipeline()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        
        services.AddSingleton(executionLog);
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        // Add short-circuit behavior first
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(
            sp => new ShortCircuitBehavior<TestRequestWithResponse, string>(executionLog));
            
        // Add another behavior that should not be executed
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(
            sp => new LoggingBehavior<TestRequestWithResponse, string>(executionLog, "ShouldNotExecute"));
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");
        
        // Act
        var result = await mediator.SendAsync(request);
        
        // Assert
        result.ShouldBe("Short-circuited");
        executionLog.Count.ShouldBe(1);
        executionLog[0].ShouldBe("Short-circuit behavior executed");
    }
    
    [Fact(DisplayName = "SendAsync void should allow behaviors to short-circuit the pipeline")]
    [Trait("Category", "Unit")]
    public async Task SendAsyncVoid_ShouldAllowBehaviorsToShortCircuitPipeline()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        
        services.AddSingleton(executionLog);
        services.AddTransient<IRequestHandler<TestRequest>>(
            sp => new TestRequestHandler(executionLog));
        
        // Add short-circuit behavior first
        services.AddTransient<IPipelineBehavior<TestRequest>>(
            sp => new ShortCircuitBehavior<TestRequest>(executionLog));
            
        // Add another behavior that should not be executed
        services.AddTransient<IPipelineBehavior<TestRequest>>(
            sp => new LoggingBehavior<TestRequest>(executionLog, "ShouldNotExecute"));
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequest("Test");
        
        // Act
        await mediator.SendAsync(request);
        
        // Assert
        executionLog.Count.ShouldBe(1);
        executionLog[0].ShouldBe("Short-circuit behavior executed");
    }
    
    [Theory(DisplayName = "SendAsync should throw when no handler is registered")]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldThrow_WhenNoHandlerIsRegistered(bool withResponse)
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        
        // Act & Assert
        if (withResponse)
        {
            var request = new TestRequestWithResponse("Test");
            await Should.ThrowAsync<InvalidOperationException>(async () => 
                await mediator.SendAsync(request));
        }
        else
        {
            var request = new TestRequest("Test");
            await Should.ThrowAsync<InvalidOperationException>(async () => 
                await mediator.SendAsync(request));
        }
    }
    
    [Fact(DisplayName = "SendAsync should execute without behaviors when none registered")]
    [Trait("Category", "Unit")]
    public async Task SendAsync_ShouldExecuteWithoutBehaviors_WhenNoneRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");
        
        // Act
        var result = await mediator.SendAsync(request);
        
        // Assert
        result.ShouldBe("Handled: Test");
    }
    
    [Fact(DisplayName = "Pipeline behaviors should handle exceptions correctly")]
    [Trait("Category", "Unit")]
    public async Task PipelineBehaviors_ShouldHandleExceptionsCorrectly()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        
        // Create a mock handler that throws an exception
        var handlerMock = new Mock<IRequestHandler<TestRequestWithResponse, string>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequestWithResponse>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
            
        services.AddTransient(provider => handlerMock.Object);
        
        // Add behavior that logs execution
        services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(
            sp => new LoggingBehavior<TestRequestWithResponse, string>(executionLog, "ExceptionBehavior"));
        
        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");
        
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => 
            await mediator.SendAsync(request));
            
        // Verify the "before" was logged but not the "after"
        executionLog.Count.ShouldBe(1);
        executionLog[0].ShouldBe("ExceptionBehavior before");
    }
    
    [Fact(DisplayName = "Multiple pipeline behaviors should be executed in correct order")]
    [Trait("Category", "Unit")]
    public async Task MultiplePipelineBehaviors_ShouldBeExecutedInCorrectOrder_RegisteredInReverseOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        
        services.AddTransient<IRequestHandler<TestRequestWithResponse, string>, TestRequestWithResponseHandler>();
        
        // Register behaviors in descending order (i = 5 down to 1)
        // Actual DI returns services in reverse order of registration.
        // With our pipeline wrapping (iterating from last to first),
        // the execution order turns out to be:
        // before: Behavior5, Behavior4, Behavior3, Behavior2, Behavior1
        // after:  Behavior1, Behavior2, Behavior3, Behavior4, Behavior5
        //
        // Therefore, update the test expectations accordingly.
        for (int i = 5; i >= 1; i--)
        {
            int capturedI = i;
            services.AddTransient<IPipelineBehavior<TestRequestWithResponse, string>>(sp =>
                new LoggingBehavior<TestRequestWithResponse, string>(
                    executionOrder, $"Behavior{capturedI}"));
        }

        var provider = services.BuildServiceProvider();
        var mediator = new MediatorImpl(provider);
        var request = new TestRequestWithResponse("Test");

        // Act
        await mediator.SendAsync(request);

        // Assert
        // Expected executionOrder:
        // 0: "Behavior5 before"
        // 1: "Behavior4 before"
        // 2: "Behavior3 before"
        // 3: "Behavior2 before"
        // 4: "Behavior1 before"
        // (Core handler executes)
        // 5: "Behavior1 after"
        // 6: "Behavior2 after"
        // 7: "Behavior3 after"
        // 8: "Behavior4 after"
        // 9: "Behavior5 after"
        executionOrder.Count.ShouldBe(10);
        executionOrder.ShouldSatisfyAllConditions(
            () => executionOrder.ElementAt(0).ShouldBe("Behavior5 before"),
            () => executionOrder.ElementAt(1).ShouldBe("Behavior4 before"),
            () => executionOrder.ElementAt(2).ShouldBe("Behavior3 before"),
            () => executionOrder.ElementAt(3).ShouldBe("Behavior2 before"),
            () => executionOrder.ElementAt(4).ShouldBe("Behavior1 before"),
            () => executionOrder.ElementAt(5).ShouldBe("Behavior1 after"),
            () => executionOrder.ElementAt(6).ShouldBe("Behavior2 after"),
            () => executionOrder.ElementAt(7).ShouldBe("Behavior3 after"),
            () => executionOrder.ElementAt(8).ShouldBe("Behavior4 after"),
            () => executionOrder.ElementAt(9).ShouldBe("Behavior5 after")
        );
    }
}
