# 🔒 NArchitecture Security Web API Components

ASP.NET Core Web API security components for Clean Architecture applications.

## ✨ Features

- 🔑 JWT Authentication Configuration
- 🛡️ Secure Token Validation
- 🔐 Comprehensive Security Checks
- ⚡ High-Performance Design
- 🎯 Easy Integration

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Security.WebApi
```

## 🚦 Quick Start

```csharp
// Configure JWT settings
var jwtConfiguration = new JwtConfiguration
{
    SecurityKey = configuration["JWT:SecurityKey"],      // Must be at least 16 chars
    Issuer = configuration["JWT:Issuer"],
    Audience = configuration["JWT:Audience"],
    AccessTokenExpiration = TimeSpan.FromMinutes(30),
    RefreshTokenTTL = TimeSpan.FromDays(7),
    ValidateIssuerSigningKey = true,
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5)
};

// Register in Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add JWT authentication with validation
    services.ConfigureJwtAuthentication(jwtConfiguration);

    // Add authorization
    services.AddAuthorization();
}

// Use in your application
public void Configure(IApplicationBuilder app)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Secure your endpoints
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Secured endpoint!");
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Security.WebApi)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
