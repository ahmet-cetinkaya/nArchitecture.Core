using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;

namespace NArchitecture.Core.Mediator.Tests;

// Marker interface for constrained requests
public interface IConstrainedRequest { }

// Test requests
file record StandardRequest(string Parameter) : IRequest<string>;

file record ConstrainedRequest(string Parameter) : IRequest<string>, IConstrainedRequest;

// Test handlers
file class StandardRequestHandler : IRequestHandler<StandardRequest, string>
{
    public Task<string> Handle(StandardRequest request, CancellationToken cancellationToken) =>
        Task.FromResult($"Standard: {request.Parameter}");
}

file class ConstrainedRequestHandler : IRequestHandler<ConstrainedRequest, string>
{
    public Task<string> Handle(ConstrainedRequest request, CancellationToken cancellationToken) =>
        Task.FromResult($"Constrained: {request.Parameter}");
}

// Helper method for getting clean type names from compiler-generated names
file static class TypeNameHelper
{
    public static string GetCleanTypeName(Type type)
    {
        // Extract the simple name without namespace and compiler-generated prefixes
        string fullName = type.Name;

        // If it's a compiler-generated name with double underscores, extract the part after the last double underscore
        int doubleUnderscoreIndex = fullName.LastIndexOf("__");
        if (doubleUnderscoreIndex >= 0)
        {
            return fullName.Substring(doubleUnderscoreIndex + 2);
        }

        return fullName;
    }
}

// Generic constrained behavior for requests implementing IConstrainedRequest
file class ConstrainedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IConstrainedRequest
{
    private readonly List<string> _executionLog;

    public ConstrainedBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _executionLog.Add($"ConstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(TRequest))}");
        return await next();
    }
}

// Generic unconstrained behavior that should apply to all requests
file class UnconstrainedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _executionLog;

    public UnconstrainedBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _executionLog.Add($"UnconstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(TRequest))}");
        return await next();
    }
}

public class MediatorImplConstrainedBehaviorTests
{
    [Fact(DisplayName = "Constrained behaviors should only execute for matching request types")]
    [Trait("Category", "Unit")]
    public async Task ConstrainedBehaviors_ShouldOnlyExecuteForMatchingRequestTypes()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executionLog);

        // Register handlers
        services.AddTransient<IRequestHandler<StandardRequest, string>, StandardRequestHandler>();
        services.AddTransient<IRequestHandler<ConstrainedRequest, string>, ConstrainedRequestHandler>();

        // Register behaviors - the constrained one will only apply to IConstrainedRequest types
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnconstrainedBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConstrainedBehavior<,>));

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var mediator = new MediatorImpl(provider, scopeFactory);

        // Act - Send standard request
        var standardRequest = new StandardRequest("Test1");
        await mediator.SendAsync(standardRequest);

        // Act - Send constrained request
        var constrainedRequest = new ConstrainedRequest("Test2");
        await mediator.SendAsync(constrainedRequest);

        // Assert
        executionLog.Count.ShouldBe(3);
        executionLog[0]
            .ShouldBe($"UnconstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(StandardRequest))}");
        executionLog[1]
            .ShouldBe($"UnconstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(ConstrainedRequest))}");
        executionLog[2]
            .ShouldBe($"ConstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(ConstrainedRequest))}");
    }

    [Fact(DisplayName = "Constrained behaviors without response should only execute for matching request types")]
    [Trait("Category", "Unit")]
    public async Task ConstrainedBehaviorsWithoutResponse_ShouldOnlyExecuteForMatchingRequestTypes()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();

        // Define void request types
        var standardVoidRequestType = typeof(StandardVoidRequest);
        var constrainedVoidRequestType = typeof(ConstrainedVoidRequest);

        services.AddSingleton(executionLog);

        // Register handlers
        services.AddTransient(
            typeof(IRequestHandler<>).MakeGenericType(standardVoidRequestType),
            typeof(StandardVoidRequestHandler)
        );
        services.AddTransient(
            typeof(IRequestHandler<>).MakeGenericType(constrainedVoidRequestType),
            typeof(ConstrainedVoidRequestHandler)
        );

        // Register behaviors - the constrained one will only apply to IConstrainedRequest types
        services.AddTransient(typeof(IPipelineBehavior<>), typeof(UnconstrainedVoidBehavior<>));
        services.AddTransient(typeof(IPipelineBehavior<>), typeof(ConstrainedVoidBehavior<>));

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var mediator = new MediatorImpl(provider, scopeFactory);

        // Act - Send standard request
        var standardRequest = new StandardVoidRequest("Test1");
        await mediator.SendAsync(standardRequest);

        // Act - Send constrained request
        var constrainedRequest = new ConstrainedVoidRequest("Test2");
        await mediator.SendAsync(constrainedRequest);

        // Assert
        executionLog.Count.ShouldBe(3);
        executionLog[0]
            .ShouldBe($"UnconstrainedVoidBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(StandardVoidRequest))}");
        executionLog[1]
            .ShouldBe(
                $"UnconstrainedVoidBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(ConstrainedVoidRequest))}"
            );
        executionLog[2]
            .ShouldBe($"ConstrainedVoidBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(ConstrainedVoidRequest))}");
    }

    [Fact(DisplayName = "Pipeline building should handle multiple constraints correctly")]
    [Trait("Category", "Unit")]
    public async Task PipelineBuilding_ShouldHandleMultipleConstraintsCorrectly()
    {
        // Arrange
        var executionLog = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executionLog);

        // Register handlers
        services.AddTransient<IRequestHandler<ConstrainedRequest, string>, ConstrainedRequestHandler>();

        // Register multiple constrained behaviors
        services.AddTransient<IPipelineBehavior<ConstrainedRequest, string>>(sp => new SpecificConstrainedBehavior(
            executionLog,
            "Specific1"
        ));
        services.AddTransient<IPipelineBehavior<ConstrainedRequest, string>>(sp => new SpecificConstrainedBehavior(
            executionLog,
            "Specific2"
        ));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConstrainedBehavior<,>));

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var mediator = new MediatorImpl(provider, scopeFactory);

        // Act
        var constrainedRequest = new ConstrainedRequest("Test");
        await mediator.SendAsync(constrainedRequest);

        // Assert - all constrained behaviors should have executed
        executionLog.Count.ShouldBe(3);
        executionLog.ShouldContain($"SpecificConstrainedBehavior Specific1 executed");
        executionLog.ShouldContain($"SpecificConstrainedBehavior Specific2 executed");
        executionLog.ShouldContain(
            $"ConstrainedBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(ConstrainedRequest))}"
        );
    }
}

// Additional test types for void requests
file record StandardVoidRequest(string Parameter) : IRequest;

file record ConstrainedVoidRequest(string Parameter) : IRequest, IConstrainedRequest;

file class StandardVoidRequestHandler : IRequestHandler<StandardVoidRequest>
{
    public Task Handle(StandardVoidRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}

file class ConstrainedVoidRequestHandler : IRequestHandler<ConstrainedVoidRequest>
{
    public Task Handle(ConstrainedVoidRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}

// Behaviors for void requests
file class UnconstrainedVoidBehavior<TRequest> : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    private readonly List<string> _executionLog;

    public UnconstrainedVoidBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        _executionLog.Add($"UnconstrainedVoidBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(TRequest))}");
        await next();
    }
}

file class ConstrainedVoidBehavior<TRequest> : IPipelineBehavior<TRequest>
    where TRequest : IRequest, IConstrainedRequest
{
    private readonly List<string> _executionLog;

    public ConstrainedVoidBehavior(List<string> executionLog)
    {
        _executionLog = executionLog;
    }

    public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        _executionLog.Add($"ConstrainedVoidBehavior executed for: {TypeNameHelper.GetCleanTypeName(typeof(TRequest))}");
        await next();
    }
}

// For testing multiple constrained behaviors
file class SpecificConstrainedBehavior : IPipelineBehavior<ConstrainedRequest, string>
{
    private readonly List<string> _executionLog;
    private readonly string _name;

    public SpecificConstrainedBehavior(List<string> executionLog, string name)
    {
        _executionLog = executionLog;
        _name = name;
    }

    public async Task<string> Handle(
        ConstrainedRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken
    )
    {
        _executionLog.Add($"SpecificConstrainedBehavior {_name} executed");
        return await next();
    }
}
