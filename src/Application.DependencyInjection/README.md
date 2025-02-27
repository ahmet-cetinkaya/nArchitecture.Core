# 🔌 NArchitecture.Core.Application.DependencyInjection

Streamlined dependency injection extensions for NArchitecture Core components with automatic discovery and registration.

## ✨ Features

- 🔍 Automatic discovery of business rule implementations
- 🧩 Assembly scanning for seamless integration
- ⚙️ Configurable service lifetime (Scoped, Transient, Singleton)
- 🔄 Fluent API for chaining registrations

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Application.DependencyInjection
```

## 🚦 Quick Start

```csharp
// Register business rules from the current assembly with default scoped lifetime
services.AddBusinessRules();

// Register from specific assemblies
services.AddBusinessRules(
    typeof(Startup).Assembly, 
    typeof(UserBusinessRules).Assembly
);

// Change service lifetime
services.AddBusinessRules(ServiceLifetime.Transient);

// Combine all options
services.AddBusinessRules(
    ServiceLifetime.Singleton,
    typeof(Program).Assembly, 
    typeof(ProductBusinessRules).Assembly
);
```

## 🧩 Business Rules Example

```csharp
// Define your business rules class
public class UserBusinessRules : IBusinessRules
{
    private readonly IUserRepository _userRepository;
    
    public UserBusinessRules(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> CheckUserExistsAsync(int id)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            throw new BusinessException("User not found");
        return user;
    }
}

// Use in your application services or handlers
public class UpdateUserCommandHandler
{
    private readonly UserBusinessRules _rules;
    
    public UpdateUserCommandHandler(UserBusinessRules rules)
    {
        _rules = rules;
    }
    
    public async Task<Unit> Handle(UpdateUserCommand command)
    {
        // Business rule validation is automatically injected
        User user = await _rules.CheckUserExistsAsync(command.Id);
        // Continue with update logic...
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Application.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
