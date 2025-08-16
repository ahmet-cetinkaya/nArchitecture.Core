# 📧 NArchitecture MailKit Integration

High-performance email sending capabilities using MailKit for Clean Architecture applications.

## ✨ Features

- 📨 SMTP support
- 🔐 DKIM signing
- 📎 Attachments
- 📝 HTML and text content
- ⚡ Bulk sending support

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mailing.MailKit
```

## 🚦 Quick Start

```csharp
// Configure mail settings
var mailSettings = new MailSettings
{
    Server = "smtp.example.com",
    Port = 587,
    UserName = "user@example.com",
    Password = "your-password",
    SenderEmail = "no-reply@example.com",
    SenderFullName = "My Application"
};

// Register mail service
services.AddSingleton(mailSettings);
services.AddScoped<IMailService, MailKitMailService>();

// Usage
public class EmailService
{
    private readonly IMailService _mailService;

    public EmailService(IMailService(IMailService mailService)
    {
        _mailService = mailService;
    }

    public async Task SendWelcomeEmail(string to)
    {
        var mail = new Mail
        {
            ToList = [new("User", to)],
            Subject = "Welcome!",
            HtmlBody = "<h1>Welcome to our platform!</h1>"
        };

        await _mailService.SendAsync(mail);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mailing.MailKit)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
