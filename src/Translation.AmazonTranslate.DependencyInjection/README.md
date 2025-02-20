# 🌐 NArchitecture Amazon Translate DI Extensions

Dependency injection extensions for Amazon Translate in Clean Architecture applications.

## ✨ Features

- 🔄 Service Registration
- ⚙️ AWS Configuration
- 🔐 Secure Credentials
- 🌍 Region Selection
- ⚡ Easy Integration

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Translation.AmazonTranslate.DependencyInjection
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Configure Amazon Translate
    services.AddAmazonTranslate(config =>
    {
        config.AccessKey = Configuration["AWS:AccessKey"];
        config.SecretKey = Configuration["AWS:SecretKey"];
        config.RegionEndpoint = Amazon.RegionEndpoint.USEast1;
    });

    // Register translation service
    services.AddScoped<ITranslationService, AmazonTranslateLocalizationManager>();
}

// Usage
public class TranslationService
{
    private readonly ITranslationService _translationService;

    public TranslationService(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task<string> TranslateToSpanish(string text)
    {
        return await _translationService.TranslateAsync(
            text: text,
            to: "es",
            from: "en"
        );
    }
}
```

## 🔐 AWS Configuration

Make sure to:
- Set up AWS credentials securely
- Configure appropriate IAM permissions
- Select the correct region endpoint
- Store sensitive data in user secrets or environment variables

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Translation.AmazonTranslate.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
