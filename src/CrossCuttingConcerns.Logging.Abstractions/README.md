# 📝 NArchitecture Logging Abstractions

Essential logging abstractions for Clean Architecture applications.

## ✨ Features

- 🎯 Common logging interfaces
- 📋 Structured logging support
- 🔄 Async logging operations
- 🛡️ Safe parameter handling
- 🔍 Detailed log context

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions
```

## 🚦 Quick Start

```csharp
public class SampleLogger : ILogger
{
    public Task LogAsync(string message, LogLevel level = LogLevel.Information)
        => Task.CompletedTask;

    public Task ErrorAsync(string message)
        => LogAsync(message, LogLevel.Error);

    public Task InformationAsync(string message)
        => LogAsync(message, LogLevel.Information);

    public Task WarningAsync(string message)
        => LogAsync(message, LogLevel.Warning);
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
