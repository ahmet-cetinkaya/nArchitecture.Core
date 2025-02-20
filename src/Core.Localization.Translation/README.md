# 🌐 NArchitecture Translation Integration

Translation-based localization for Clean Architecture applications.

## ✨ Features

- 🔄 Dynamic translation support
- 🌍 Multiple language handling
- 🎯 Fallback language support
- ⚡ High-performance translation
- 🛡️ Thread-safe operations

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Localization.Translation
```

## 🚦 Quick Start

```csharp
// Configure translation service
services.AddScoped<ITranslationService, CustomTranslationService>();

// Configure localization
services.AddScoped<ILocalizationService, TranslateLocalizationManager>();

// Usage
public class TranslatedService
{
    private readonly ILocalizationService _localization;

    public TranslatedService(ILocalizationService localization)
    {
        _localization = localization;
        _localization.AcceptLocales = ["tr-TR", "en-US"];
    }

    public async Task<string> GetGreeting()
    {
        return await _localization.GetLocalizedAsync("Hello");
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Localization.Translation)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
