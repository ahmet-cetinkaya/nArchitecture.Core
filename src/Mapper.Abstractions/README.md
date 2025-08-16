# 🗺️ NArchitecture Mapper Abstractions

Mapping abstraction layer for Clean Architecture applications.

## ✨ Features

- 🔄 Standardized mapping interfaces
- 🧩 Implementation agnostic design
- 📈 Dependency inversion for mapping operations
- 🛠️ Support for customized mapping scenarios

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mapper.Abstractions
```

## 🚦 Quick Start

```csharp
// Inject mapping service
public class UserService
{
    private readonly IMapper _mapper;

    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public UserDto GetUser(int id)
    {
        var user = _repository.GetById(id);
        return _mapper.Map<UserDto>(user);
    }
}
```

## Available Interfaces

### IMapper

The core mapping interface that provides mapping functionality between objects.

```csharp
public interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mapper.Abstractions)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
