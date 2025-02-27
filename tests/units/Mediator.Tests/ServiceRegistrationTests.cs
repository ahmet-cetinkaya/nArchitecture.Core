using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Abstractions;
using NArchitecture.Core.Mediator.Abstractions.CQRS;

namespace NArchitecture.Core.Mediator.Tests;

// Keep test classes separate with clear names
file record TestRequest : IRequest;
file class TestRequestHandler : IRequestHandler<TestRequest>
{
    public Task Handle(TestRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}

file record TestQuery : IQuery<string>;
file class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery request, CancellationToken cancellationToken) => Task.FromResult("Result");
}

file record TestCommand : ICommand;
file class TestCommandHandler : ICommandHandler<TestCommand>
{
    public Task Handle(TestCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}

// Create separate event types for different test scenarios
file record TestEvent : IEvent;
file record MultipleHandlersEvent : IEvent;

file class TestEventHandler : IEventHandler<TestEvent>
{
    public Task Handle(TestEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

file class FirstMultipleHandlersEventHandler : IEventHandler<MultipleHandlersEvent>
{
    public Task Handle(MultipleHandlersEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

file class SecondMultipleHandlersEventHandler : IEventHandler<MultipleHandlersEvent>
{
    public Task Handle(MultipleHandlersEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

// Add a test behavior class to be registered
public class TestRequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}

public class ServiceRegistrationTests
{
    [Fact(DisplayName = "AddMediator should register IMediator implementation")]
    [Trait("Category", "Unit")]
    public void AddMediator_ShouldRegisterMediatorImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(ServiceRegistrationTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        mediator.ShouldNotBeNull();
        mediator.ShouldBeOfType<MediatorImpl>();
    }

    [Fact(DisplayName = "AddMediator should register handlers from provided assemblies")]
    [Trait("Category", "Unit")]
    public void AddMediator_ShouldRegisterHandlersFromProvidedAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(TestRequestHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IRequestHandler<TestRequest>>();
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestRequestHandler>();

        var queryHandler = provider.GetService<IQueryHandler<TestQuery, string>>();
        queryHandler.ShouldNotBeNull();
        queryHandler.ShouldBeOfType<TestQueryHandler>();

        var commandHandler = provider.GetService<ICommandHandler<TestCommand>>();
        commandHandler.ShouldNotBeNull();
        commandHandler.ShouldBeOfType<TestCommandHandler>();

        var eventHandler = provider.GetService<IEventHandler<TestEvent>>();
        eventHandler.ShouldNotBeNull();
        eventHandler.ShouldBeOfType<TestEventHandler>();
    }

    [Fact(DisplayName = "AddMediator should register all handler implementations for same interface")]
    [Trait("Category", "Unit")]
    public void AddMediator_ShouldRegisterAllHandlerImplementationsForSameInterface()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(FirstMultipleHandlersEventHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IEventHandler<MultipleHandlersEvent>>().ToList();
        handlers.Count.ShouldBe(2);
        handlers.ShouldContain(h => h.GetType() == typeof(FirstMultipleHandlersEventHandler));
        handlers.ShouldContain(h => h.GetType() == typeof(SecondMultipleHandlersEventHandler));
    }

    [Fact(DisplayName = "AddMediator should register pipeline behaviors from provided assemblies")]
    [Trait("Category", "Unit")]
    public void AddMediator_ShouldRegisterPipelineBehaviorsFromProvidedAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(typeof(TestRequestBehavior<,>).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var behaviorType = typeof(IPipelineBehavior<TestQuery, string>);
        var behavior = provider.GetService<IPipelineBehavior<TestQuery, string>>();
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<TestRequestBehavior<TestQuery, string>>();
    }
}
