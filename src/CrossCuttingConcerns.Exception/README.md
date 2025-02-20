# 🛡️ NArchitecture Exception Handling

Professional exception handling components for Clean Architecture applications.

## ✨ Features

- 📋 Standardized exception types
- 🔄 Middleware integration
- 🎯 Consistent error handling
- 🚦 Request validation
- 🔐 Authorization checks

## 📥 Installation 

```bash
dotnet add package NArchitecture.Core.CrossCuttingConcerns.Exception
```

## 🚦 Quick Start

```csharp
// Using custom exceptions
throw new BusinessException("Invalid operation.");
throw new ValidationException("Validation failed.");
throw new AuthorizationException("Access denied.");

// Registering middleware
builder.Services.AddExceptionHandler();

// Using middleware
app.UseExceptionHandler();
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.CrossCuttingConcerns.Exception)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
