# ✅ NArchitecture Validation Abstractions

Essential validation abstractions for Clean Architecture applications.

## ✨ Features

- 🎯 Generic Validator Interface
- 📝 Strongly-Typed Results
- 🔍 Property-Level Errors
- ⚡ High Performance
- 🛠️ Easy Integration

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Validation.Abstractions
```

## 🚦 Quick Start

```csharp
// Define your validator
public class UserValidator : IValidator<User>
{
    public ValidationResult Validate(User user)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrEmpty(user.Name))
        {
            errors.Add(new ValidationError(
                PropertyName: nameof(user.Name),
                Errors: new[] { "Name is required" }
            ));
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            errors.Add(new ValidationError(
                PropertyName: nameof(user.Email),
                Errors: new[] { "Email is required" }
            ));
        }

        return new ValidationResult(
            IsValid: !errors.Any(),
            Errors: errors
        );
    }
}

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
        var validationResult = _validator.Validate(user);
        
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors!
                    .SelectMany(e => e.Errors)
                    .ToArray()
            );
        }

        // Continue with user creation...
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Validation.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
