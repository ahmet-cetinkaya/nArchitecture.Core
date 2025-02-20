# 🌐 NArchitecture Translation Abstractions

Essential translation service abstractions for Clean Architecture applications.

## ✨ Features

- 🔄 Language Translation
- 🌍 Multi-Language Support
- 🎯 Provider-Agnostic Design
- ⚡ Async Operations
- 🛠️ Easy Integration

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Translation.Abstractions
```

## 🚦 Quick Start

```csharp
// Implement translation service
public class GoogleTranslationService : ITranslationService
{
    public async Task<string> TranslateAsync(
        string text, 
        string to, 
        string from = "en")
    {
        // Implementation using Google Translate API
    }
}

// Register in DI
services.AddScoped<ITranslationService, GoogleTranslationService>();

// Usage
public class TranslationService
{
    private readonly ITranslationService _translationService;

    public TranslationService(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task<string> TranslateToTurkish(string text)
    {
        return await _translationService.TranslateAsync(
            text: text,
            to: "tr",
            from: "en"
        );
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Translation.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
