# 🚀 NArchitecture.Core.Application

Essential application layer components for Clean Architecture with built-in Mediator pipeline behaviors.

## ✨ Features

- 🛡️ Validation Pipeline
- 📦 Caching Pipeline (with distributed cache support)
- 💾 Transaction Management
- 🔐 Authorization Pipeline
- ⚡ Performance Monitoring
- 📝 Structured Logging

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Application
```

## 🚦 Quick Start

1. Register the pipeline behaviors:

```csharp
services.AddMediator(cfg => {
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionScopeBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
});
```

2. Use in your requests:

```csharp
// Cacheable request
public class GetUserQuery : IRequest<UserDto>, ICacheableRequest
{
    public string UserId { get; set; }
    public CacheableOptions CacheOptions => new() 
    { 
        CacheKey = $"user-{UserId}",
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };
}

// Secured request
public class DeleteUserCommand : IRequest<bool>, ISecuredRequest
{
    public string UserId { get; set; }
    public AuthOptions AuthOptions => new(CurrentUser.Roles, ["Admin"]);
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Application)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
