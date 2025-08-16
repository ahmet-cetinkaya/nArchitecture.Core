# 💉 NArchitecture Entity Framework DI Extensions

Dependency injection extensions for Entity Framework Core in Clean Architecture applications.

## ✨ Features

- 🔄 Database Migration Support
- 🏭 Automatic Service Registration
- 📦 Context Registration
- ⚡ Performance Optimizations
- 🛡️ Scoped Lifetime Management

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Persistence.EntityFramework.DependencyInjection
```

## 🚦 Quick Start

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register your DbContext
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

    // Register migration applier
    services.AddDbMigrationApplier<AppDbContext>(sp => 
        sp.GetRequiredService<AppDbContext>());
}

public void Configure(IApplicationBuilder app)
{
    // Apply migrations on startup
    app.ApplicationServices.UseDbMigrationApplier();
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Persistence.EntityFramework.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
