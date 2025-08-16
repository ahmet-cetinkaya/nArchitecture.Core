# 🌐 NArchitecture YAML Localization DI Extensions

Dependency injection extensions for YAML-based localization in Clean Architecture applications.

## ✨ Features

- 🔄 Automatic resource discovery (embedded + file system)
- 📁 Convention-based file organization
- 🎯 Feature-based localization
- ⚡ Efficient service registration
- 🛡️ Type-safe configuration
- 📦 Embedded resource support for deployment resilience

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add YAML-based localization with assembly scanning
    services.AddYamlResourceLocalization(Assembly.GetExecutingAssembly());
}

// Embedded resources (preferred - robust deployment):
// Features.Index.Resources.Locales.index.en.yaml
// Features.Index.Resources.Locales.index.tr.yaml  
// Features.Users.Resources.Locales.users.en.yaml
// Features.Users.Resources.Locales.users.tr.yaml

// File system fallback structure:
// Features/
//   ├── Index/
//   │   └── Resources/
//   │       └── Locales/
//   │           ├── index.en.yaml
//   │           └── index.tr.yaml
//   └── Users/
//       └── Resources/
//           └── Locales/
//               ├── users.en.yaml
//               └── users.tr.yaml
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
