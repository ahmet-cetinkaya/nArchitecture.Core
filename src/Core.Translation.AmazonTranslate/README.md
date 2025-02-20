# 🌐 NArchitecture Amazon Translate Integration

Amazon Translate integration for Clean Architecture applications.

## ✨ Features

- 🔄 AWS Translation Service
- 🌍 Multiple Language Support
- 🔐 Secure Authentication
- 🎯 Region Configuration
- ⚡ High Performance

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Translation.AmazonTranslate
```

## 🚦 Quick Start

```csharp
// Configure AWS settings
var config = new AmazonTranslateConfiguration(
    AccessKey: configuration["AWS:AccessKey"],
    SecretKey: configuration["AWS:SecretKey"],
    RegionEndpoint: RegionEndpoint.USEast1
);

// Register in DI
services.AddSingleton(config);
services.AddScoped<ITranslationService, AmazonTranslateLocalizationManager>();

// Usage
public class TranslationService
{
    private readonly ITranslationService _translationService;

    public TranslationService(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task<string> TranslateContent(string content, string targetLanguage)
    {
        return await _translationService.TranslateAsync(
            text: content,
            to: targetLanguage,
            from: "en"
        );
    }
}
```

## 🔑 AWS Configuration

Ensure you have:
- AWS Account with Amazon Translate access
- IAM user with appropriate permissions
- Access key and secret key
- Selected AWS region endpoint

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Translation.AmazonTranslate)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
