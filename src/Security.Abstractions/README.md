# 🔒 NArchitecture Security Abstractions

Essential security abstractions for Clean Architecture applications.

## ✨ Features

- 🔐 Authentication Interfaces
- 🛡️ Authorization Contracts
- 👥 User Management
- 🎫 Token Operations
- 📱 2FA Support
- 🔑 Cryptography Interfaces

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Security.Abstractions
```

## 🚦 Quick Start

```csharp
// Implement the authentication service
public class AuthenticationManager<TUserId, TUserAuthenticatorId> 
    : IAuthenticationService<TUserId, TUserAuthenticatorId>
{
    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest<TUserId, TUserAuthenticatorId> request,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    // Other implementations...
}

// Register in DI
services.AddScoped<IAuthenticationService<Guid, int>, AuthenticationManager<Guid, int>>();

// Usage
public class LoginCommandHandler
{
    private readonly IAuthenticationService<Guid, int> _authService;

    public async Task<AuthenticationResponse> Handle(LoginCommand command)
    {
        var request = new LoginRequest<Guid, int>
        {
            UserId = command.UserId,
            Password = command.Password,
            IpAddress = command.IpAddress
        };

        return await _authService.LoginAsync(request);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Security.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
