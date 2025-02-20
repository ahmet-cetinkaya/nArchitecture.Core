# 🌐 NArchitecture Localization Abstractions

Essential localization abstractions for Clean Architecture applications.

## ✨ Features

- 🔤 Culture-based localization
- 📦 Resource abstraction
- 🔄 Dynamic locale switching
- 🎯 Section-based organization
- ⚡ High-performance design

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Localization.Abstractions
```

## 🚦 Quick Start

```csharp
public class LocalizedService
{
    private readonly ILocalizationService _localization;

    public LocalizedService(ILocalizationService localization)
    {
        _localization = localization;
        _localization.AcceptLocales = ["en-US", "tr-TR"];
    }

    public async Task<string> GetWelcomeMessage()
    {
        return await _localization.GetLocalizedAsync("Welcome", "Messages");
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Localization.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
