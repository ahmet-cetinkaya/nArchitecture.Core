# 🔌 NArchitecture FluentValidation Dependency Injection

Dependency Injection extension methods for seamless integration of FluentValidation in Clean Architecture applications.

## ✨ Features

- 🚀 Simple service registration
- 📦 Assembly scanning using marker interfaces
- 🔄 Automatic adapter registration
- 🔗 Clean integration with Microsoft DI
- 🌐 Supports existing FluentValidation validators

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Validation.FluentValidation.DependencyInjection
```

## 🚦 Quick Start

### Register Validators Using Marker Interface

The recommended way is to mark your validator classes with `IValidationProfile` interface:

```csharp
using FluentValidation;
using NArchitecture.Core.Validation.Abstractions;

// Mark your validator with the interface
public class UserValidator : AbstractValidator<User>, IValidationProfile
{
    public UserValidator()
    {
        RuleFor(u => u.Email).NotEmpty().EmailAddress();
        RuleFor(u => u.Name).NotEmpty().MinimumLength(2);
    }
}
```

Then register them:

```csharp
using NArchitecture.Core.Validation.FluentValidation.DependencyInjection;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register validators that implement IValidationProfile
builder.Services.AddFluentValidation(
    new[] { typeof(Program).Assembly, typeof(UserValidator).Assembly }
);
```

### Register All Validators Without Marker Interface

If you prefer to register all validators without using the marker interface:

```csharp
// Register all FluentValidation validators without filtering
builder.Services.AddAllFluentValidators(
    new[] { typeof(Program).Assembly }
);
```

### Register Individual Validators

```csharp
using NArchitecture.Core.Validation.FluentValidation.DependencyInjection;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register individual validators
builder.Services.AddFluentValidator<User>();

// Then register your concrete FluentValidation validator separately
builder.Services.AddScoped<FluentValidation.IValidator<User>, UserValidator>();
```

## 📝 Examples

### Using Different Service Lifetime

```csharp
// Register validators with transient lifetime
builder.Services.AddFluentValidation(
    new[] { typeof(Program).Assembly },
    ServiceLifetime.Transient
);
```

### Using Registered Validators

```csharp
public class CreateUserHandler
{
    private readonly NArchitecture.Core.Validation.Abstractions.IValidator<User> _validator;
    
    public CreateUserHandler(NArchitecture.Core.Validation.Abstractions.IValidator<User> validator)
    {
        _validator = validator;
    }
    
    public async Task<Result> Handle(CreateUserCommand command)
    {
        var user = new User(command.Name, command.Email);
        
        // Use the registered validator
        var validationResult = _validator.Validate(user);
        if (!validationResult.IsValid)
        {
            return Result.Failure(validationResult.Errors);
        }
        
        // Continue with valid user...
        return Result.Success();
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Validation.FluentValidation.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
- 📘 [FluentValidation Documentation](https://docs.fluentvalidation.net)
