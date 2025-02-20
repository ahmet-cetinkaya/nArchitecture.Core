# 📝 NArchitecture Serilog Integration

Serilog integration for Clean Architecture applications with structured logging support.

## ✨ Features

- 🔄 Easy Serilog configuration
- 📊 Structured logging
- 🎯 Multiple sink support
- 🛡️ Safe logging practices
- ⚡ High-performance logging

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog
```

## 🚦 Quick Start

```csharp
public class FileLogger : SerilogLoggerServiceBase
{
    public FileLogger()
        : base(new LoggerConfiguration()
            .WriteTo.File("logs/log.txt")
            .CreateLogger())
    {
    }
}

// Register in DI
services.AddLogging(new FileLogger());
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
