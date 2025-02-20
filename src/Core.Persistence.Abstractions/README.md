# 💾 NArchitecture Persistence Abstractions

Essential persistence abstractions for Clean Architecture applications.

## ✨ Features

- 🏭 Generic Repository Pattern
- 📑 Pagination Support
- 🔍 Dynamic Queries
- 📦 Bulk Operations
- 🔄 Migration Management

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Persistence.Abstractions
```

## 🚦 Quick Start

```csharp
// Define your entity
public class User : BaseEntity<Guid>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Implement repository
public class UserRepository : IAsyncRepository<User, Guid>
{
    // Implementation details...
}

// Usage
public class UserService
{
    private readonly IAsyncRepository<User, Guid> _userRepository;

    public UserService(IAsyncRepository<User, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IPaginate<User>> GetUsers(int page, int size)
    {
        return await _userRepository.GetListAsync(
            predicate: u => u.Name.Contains("John"),
            orderBy: q => q.OrderBy(u => u.Name),
            index: page,
            size: size
        );
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Persistence.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
