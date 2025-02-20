# 🔒 NArchitecture Security DI Extensions

Dependency injection extensions for security services in Clean Architecture applications.

## ✨ Features

- 🔐 JWT Authentication Registration
- 👥 Authorization Service Setup
- 📱 2FA Configuration
- 🔑 OTP Service Registration
- 📧 Email Verification Setup
- 📱 SMS Verification Setup
- 🛡️ Cryptography Services

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Security.DependencyInjection
```

## 🚦 Quick Start

```csharp
// Configure JWT settings
var jwtConfig = new JwtConfiguration
{
    Secret = configuration["JWT:Secret"],
    Issuer = configuration["JWT:Issuer"],
    Audience = configuration["JWT:Audience"],
    AccessTokenExpiration = 30
};

// Configure authenticator settings
var authConfig = new AuthenticatorConfiguration
{
    EnabledAuthenticatorTypes = [AuthenticatorType.Email, AuthenticatorType.Otp],
    CodeLength = 6,
    CodeValidityDuration = TimeSpan.FromMinutes(5)
};

// Register security services
services.AddSecurityServices<Guid, int, int, Guid>(
    jwtConfiguration: jwtConfig,
    authenticatorConfiguration: authConfig
);

// Usage
public class AuthService
{
    private readonly IAuthenticationService<Guid, int> _authService;
    private readonly IAuthenticator<Guid, int> _authenticator;

    public async Task<AuthResponse> LoginWithTwoFactor(LoginRequest request)
    {
        var authResult = await _authService.LoginAsync(request);
        
        if (authResult.RequiresTwoFactor)
        {
            await _authenticator.AttemptAsync(
                request.UserId,
                request.Email
            );
        }

        return authResult;
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Security.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
