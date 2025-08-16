# 📱 NArchitecture SMS Abstractions

Essential SMS service abstractions for Clean Architecture applications.

## ✨ Features

- 📨 SMS Service Interface
- 🔄 Bulk SMS Support
- 🎯 Custom Parameters
- ⚡ Priority Levels
- 🌐 International Format

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Sms.Abstractions
```

## 🚦 Quick Start

```csharp
// Implement SMS service
public class TwilioSmsService : ISmsService
{
    public async Task SendAsync(Sms sms, CancellationToken cancellationToken = default)
    {
        // Implementation for sending single SMS
    }

    public async Task SendBulkAsync(IEnumerable<Sms> smsList, CancellationToken cancellationToken = default)
    {
        // Implementation for sending multiple SMS
    }
}

// Register in DI
services.AddScoped<ISmsService, TwilioSmsService>();

// Usage
public class NotificationService
{
    private readonly ISmsService _smsService;

    public NotificationService(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public async Task SendVerificationCode(string phoneNumber, string code)
    {
        var sms = new Sms(
            PhoneNumber: phoneNumber,
            Content: $"Your verification code is: {code}"
        ) 
        {
            Priority = 1, // High priority
            CustomParameters = new()
            {
                ["type"] = "verification",
                ["expiresIn"] = "5m"
            }
        };

        await _smsService.SendAsync(sms);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Sms.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
