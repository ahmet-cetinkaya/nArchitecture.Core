# 💾 NArchitecture Entity Framework Core Integration

High-performance Entity Framework Core integration for Clean Architecture applications.

## ✨ Features

- 🏭 Generic Repository Implementation
- 📑 Advanced Pagination
- 🎯 Dynamic Query Support
- 🔄 Optimistic Concurrency
- ⚡ Bulk Operations
- 🛡️ Soft Delete Support

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Persistence.EntityFramework
```

## 🚦 Quick Start

```csharp
// Define your DbContext
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}

// Implement repository
public class UserRepository : EfRepositoryBase<User, Guid, AppDbContext>
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }
}

// Register in DI
services.AddDbContext<AppDbContext>();
services.AddScoped<IAsyncRepository<User, Guid>, UserRepository>();

// Usage
public class UserService
{
    private readonly IAsyncRepository<User, Guid> _userRepository;

    public UserService(IAsyncRepository<User, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IPaginate<User>> GetActiveUsers(int page, int size)
    {
        return await _userRepository.GetListAsync(
            predicate: u => !u.DeletedAt.HasValue,
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            index: page,
            size: size
        );
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Persistence.EntityFramework)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
