# 📧 NArchitecture Mailing Abstractions

Essential mailing abstractions for Clean Architecture applications.

## ✨ Features

- 📥 Email sending abstractions
- 🔄 Async operations
- 📨 Bulk mail support
- 🎯 Template support
- ⚡ High-performance design

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mailing.Abstractions
```

## 🚦 Quick Start

```csharp
// Implement mail service
public class SmtpMailService : IMailService
{
    public async Task SendAsync(Mail mail, CancellationToken cancellationToken = default)
    {
        // Implementation for sending single email
    }

    public async Task SendBulkAsync(IEnumerable<Mail> mailList, CancellationToken cancellationToken = default)
    {
        // Implementation for sending multiple emails
    }
}

// Register in DI
services.AddScoped<IMailService, SmtpMailService>();

// Usage
public class NotificationService
{
    private readonly IMailService _mailService;

    public NotificationService(IMailService mailService)
    {
        _mailService = mailService;
    }

    public async Task SendWelcomeEmail(string to)
    {
        var mail = new Mail
        {
            ToEmail = to,
            Subject = "Welcome!",
            TextBody = "Welcome to our platform."
        };

        await _mailService.SendAsync(mail);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mailing.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
