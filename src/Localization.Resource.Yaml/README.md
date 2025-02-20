# 🌐 NArchitecture YAML Resource Provider

YAML-based resource management for localization in Clean Architecture applications.

## ✨ Features

- 📄 YAML resource support
- 🔄 Dynamic resource loading
- 📦 Section-based organization
- 🎯 Lazy loading optimization
- ⚡ High-performance parsing

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Localization.Resource.Yaml
```

## 🚦 Quick Start

```csharp
// Configure resource paths
var resources = new Dictionary<string, Dictionary<string, string>>
{
    ["en"] = new()
    {
        ["messages"] = "Resources/en/messages.yaml",
        ["errors"] = "Resources/en/errors.yaml"
    },
    ["tr"] = new()
    {
        ["messages"] = "Resources/tr/messages.yaml",
        ["errors"] = "Resources/tr/errors.yaml"
    }
};

// Create and register localization service
services.AddSingleton<ILocalizationService>(new ResourceLocalizationManager(resources));
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Localization.Resource.Yaml)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
