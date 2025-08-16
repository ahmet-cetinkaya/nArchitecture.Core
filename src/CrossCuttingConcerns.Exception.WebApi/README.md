# 🛡️ NArchitecture Exception Handling for Web API

Exception handling middleware and components for ASP.NET Web API applications.

## ✨ Features

- 🔄 Global exception middleware
- 🎯 Automatic exception handling
- 📝 Structured error responses
- 🔍 Detailed logging support
- ⚡ High-performance handlers

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void Configure(IApplicationBuilder app)
{
    // Add the exception middleware to the pipeline
    app.ConfigureCustomExceptionMiddleware();
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
