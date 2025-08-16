# NArchitecture.Core.Mediator

Implementation of the mediator pattern for Clean/Union Architecture, providing concrete implementations for commands, queries, and events.

## Installation

```bash
dotnet add package NArchitecture.Core.Mediator
```

## Usage

### Register Services

```csharp
// In Program.cs or Startup.cs
builder.Services.AddMediator(typeof(Program).Assembly);
```

### Creating and Handling Requests

```csharp
// Define a query
public record GetUserByIdQuery(Guid Id) : IQuery<UserDto>;

// Implement a query handler
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        return new UserDto { Id = user.Id, Name = user.Name };
    }
}

// Use the mediator in a controller or service
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _mediator.SendAsync(new GetUserByIdQuery(id));
        return Ok(result);
    }
}
```

### Working with Events

```csharp
// Define an event
public record UserCreatedEvent(Guid Id, string Name) : IEvent;

// Implement an event handler
public class EmailNotificationHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;

    public EmailNotificationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(@event.Name, cancellationToken);
    }
}

// Publish an event
await _mediator.PublishAsync(new UserCreatedEvent(user.Id, user.Name));
```

### Working with Streaming Requests

Streaming requests allow you to return a stream of data using `IAsyncEnumerable<T>`, which is useful for scenarios like real-time data feeds, large dataset processing, or server-sent events.

```csharp
// Define a streaming request
public record GetUsersStreamQuery(string? Filter = null) : IStreamRequest<UserDto>;

// Implement a streaming request handler
public class GetUsersStreamQueryHandler : IStreamRequestHandler<GetUsersStreamQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUsersStreamQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async IAsyncEnumerable<UserDto> Handle(
        GetUsersStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var user in _userRepository.GetUsersStreamAsync(request.Filter, cancellationToken))
        {
            yield return new UserDto { Id = user.Id, Name = user.Name };
        }
    }
}

// Use streaming in a controller
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stream")]
    public async IAsyncEnumerable<UserDto> GetUsersStream(
        [FromQuery] string? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var user in _mediator.SendStreamAsync(new GetUsersStreamQuery(filter), cancellationToken))
        {
            yield return user;
        }
    }
}
```

### Streaming Pipeline Behaviors

You can create pipeline behaviors for streaming requests to add cross-cutting concerns like logging, validation, or caching:

```csharp
public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly ILogger<StreamLoggingBehavior<TRequest, TResponse>> _logger;

    public StreamLoggingBehavior(ILogger<StreamLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<TResponse> Handle(
        TRequest request,
        RequestStreamHandlerDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting stream for {RequestType}", typeof(TRequest).Name);
        
        var itemCount = 0;
        await foreach (var item in next().WithCancellation(cancellationToken))
        {
            itemCount++;
            _logger.LogDebug("Streaming item {ItemCount} for {RequestType}", itemCount, typeof(TRequest).Name);
            yield return item;
        }
        
        _logger.LogInformation("Completed stream for {RequestType} with {ItemCount} items", typeof(TRequest).Name, itemCount);
    }
}
```

## License

This project is licensed under the terms of the LICENSE file included in the repository.
