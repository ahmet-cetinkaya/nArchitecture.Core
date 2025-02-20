# 🔒 NArchitecture Security Components

Comprehensive security components for Clean Architecture applications.

## ✨ Features

- 🔑 JWT Authentication
- 👥 Role-based Authorization
- 🔐 Two-Factor Authentication
- 📱 OTP Support
- 🛡️ Password Hashing
- 🔒 Encryption Utilities
- 📧 Email Verification
- 📱 SMS Verification

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Security
```

## 🚦 Quick Start

```csharp
// Configure JWT settings
services.Configure<TokenOptions>(configuration.GetSection("TokenOptions"));

// Register security services
services.AddScoped<ITokenHelper, JwtHelper>();
services.AddScoped<IEmailAuthenticator, EmailAuthenticator>();
services.AddScoped<IOtpAuthenticator, OtpAuthenticator>();

// Usage example
public class AuthService
{
    private readonly ITokenHelper _tokenHelper;
    private readonly IEmailAuthenticator _emailAuthenticator;
    
    public AuthService(ITokenHelper tokenHelper, IEmailAuthenticator emailAuthenticator)
    {
        _tokenHelper = tokenHelper;
        _emailAuthenticator = emailAuthenticator;
    }

    public async Task<AccessToken> Login(User user)
    {
        // Generate JWT token
        var token = _tokenHelper.CreateToken(user, userClaims);

        // Send 2FA code if enabled
        if (user.TwoFactorEnabled)
        {
            var code = await _emailAuthenticator.CreateEmailActivationCode();
            await _emailAuthenticator.SendAuthenticatorCode(user.Email, code);
        }

        return token;
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Security)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
