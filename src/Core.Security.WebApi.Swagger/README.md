# 📚 NArchitecture Security Swagger Integration

Swagger integration for security features in Clean Architecture applications.

## ✨ Features

- 🔐 JWT Authentication Documentation
- 🛡️ Security Scheme Configuration
- 📝 Bearer Token Integration
- 🎯 Operation Filters
- ⚡ Easy Setup

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Security.WebApi.Swagger
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddSwaggerGen(options =>
    {
        // Add security definition
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        // Add security requirements
        options.OperationFilter<BearerTokenSecurityOperationFilter>();
    });
}

public void Configure(IApplicationBuilder app)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.DocumentTitle = "API Documentation";
    });
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Security.WebApi.Swagger)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
