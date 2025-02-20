# 🌐 NArchitecture Web API Localization

ASP.NET Web API localization support for Clean Architecture applications.

## ✨ Features

- 🔄 Auto language detection
- 🌍 Accept-Language support
- 🎯 Middleware integration
- ⚡ High-performance design
- 🛡️ Thread-safe operations

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Localization.WebApi
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void Configure(IApplicationBuilder app)
{
    // Add the localization middleware
    app.UseLocalization();
}

// Controller
public class UsersController : ControllerBase
{
    private readonly ILocalizationService _localization;

    public UsersController(ILocalizationService localization)
    {
        _localization = localization;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Will use Accept-Language header automatically
        string message = await _localization.GetLocalizedAsync("Welcome");
        return Ok(message);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Localization.WebApi)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
