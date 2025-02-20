# ✅ NArchitecture FluentValidation Integration

FluentValidation integration for Clean Architecture applications.

## ✨ Features

- 🔍 Fluent Validation Adapter
- 🎯 Type-Safe Validation
- 🔄 Easy Integration
- ⚡ High Performance
- 🛠️ Custom Rule Support

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Validation.FluentValidation
```

## 🚦 Quick Start

```csharp
// Define your FluentValidation validator
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(u => u.Name)
            .NotEmpty()
            .MinimumLength(2);
    }
}

// Register in DI
services.AddScoped<IValidator<User>, FluentValidatorAdapter<User>>();
services.AddScoped<FluentValidation.IValidator<User>, UserValidator>();

// Usage
public class UserService
{
    private readonly IValidator<User> _validator;

    public UserService(IValidator<User> validator)
    {
        _validator = validator;
    }

    public void CreateUser(User user)
    {
        var result = _validator.Validate(user);
        
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => $"{e.PropertyName}: {e.Errors.First()}")
                .ToArray();
                
            throw new ValidationException(errors);
        }

        // Continue with valid user...
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Validation.FluentValidation)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
