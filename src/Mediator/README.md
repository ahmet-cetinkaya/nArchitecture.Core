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

## License

This project is licensed under the terms of the LICENSE file included in the repository.
