# NArchitecture Core Mediator Abstractions

This package provides abstractions for implementing the Mediator pattern in .NET applications following Clean Architecture principles.

## Features

- Core interfaces for implementing a mediator pattern
- Support for command-query separation (CQRS)
- Event handling abstractions
- Framework-agnostic design

## Usage

```csharp
// Example with query:
public class GetUserByIdQuery : IQuery<UserDto>
{
    public GetUserByIdQuery(Guid id) => Id = id;
    public Guid Id { get; }
}

// Example with command:
public class CreateUserCommand : ICommand<CreatedUserResponse> 
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

// Handling a request:
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Using the mediator:
public class UserController
{
    private readonly IMediator _mediator;
    
    public UserController(IMediator mediator) => _mediator = mediator;
    
    public async Task<UserDto> GetUserById(Guid id)
    {
        return await _mediator.Send(new GetUserByIdQuery(id));
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
