# 📝 NArchitecture Serilog File Integration

File-based logging implementation with Serilog for Clean Architecture applications.

## ✨ Features

- 📁 File-based logging
- 🔄 Log file rotation
- 📊 Structured log format
- ⚡ High-performance logging
- 🛡️ Thread-safe operations

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File
```

## 🚦 Quick Start

```csharp
// Configure file logger
var config = new SerilogFileLogConfiguration(
    folderPath: "logs",
    rollingInterval: RollingInterval.Day,
    fileSizeLimitBytes: 10_000_000
);

// Create and register logger
services.AddLogging(new SerilogFileLogger(config));
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.CrossCuttingConcerns.Logging.Serilog.File)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
