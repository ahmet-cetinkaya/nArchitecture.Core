# 🔌 NArchitecture AutoMapper Dependency Injection

Dependency Injection extension methods for seamless integration of AutoMapper in Clean Architecture applications.

## ✨ Features

- 🚀 Simple service registration
- 📦 Profile scanning using marker interfaces
- 🔄 Automatic adapter registration
- 🔗 Clean integration with Microsoft DI
- 🌐 Supports existing AutoMapper profiles

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mapper.AutoMapper.DependencyInjection
```

## 🚦 Quick Start

### Register Profiles Using Marker Interface

The recommended way is to mark your profile classes with `IMappingProfile` interface:

```csharp
using AutoMapper;
using NArchitecture.Core.Mapper.Abstractions;

// Mark your profile with the interface
public class UserMappingProfile : Profile, IMappingProfile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
```

Then register them:

```csharp
using NArchitecture.Core.Mapper.AutoMapper.DependencyInjection;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register only profiles that implement IMappingProfile
builder.Services.AddAutoMapper(
    typeof(Program).Assembly, 
    typeof(UserMappingProfile).Assembly
);
```

### Register All Profiles Without Marker Interface

If you prefer to register all profiles without using the marker interface:

```csharp
// Register all AutoMapper profiles without filtering
builder.Services.AddAllAutoMapperProfiles(typeof(Program).Assembly);
```

## 📝 Examples

### Adding Custom AutoMapper Configuration

```csharp
// Register with custom configuration
builder.Services.AddAutoMapper(
    cfg => 
    {
        // Custom AutoMapper configuration here
        cfg.AllowNullCollections = true;
        cfg.AllowNullDestinationValues = true;
    },
    typeof(Program).Assembly
);
```

### Using Different Service Lifetime

```csharp
// Register with transient lifetime
builder.Services.AddAutoMapper(
    ServiceLifetime.Transient,
    typeof(Program).Assembly
);
```

### Using the Mapper in Services

```csharp
public class UserService
{
    private readonly NArchitecture.Core.Mapper.Abstractions.IMapper _mapper;
    
    public UserService(NArchitecture.Core.Mapper.Abstractions.IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUserById(int id)
    {
        var user = _repository.GetById(id);
        
        // Use the mapper adapter
        return _mapper.Map<UserDto>(user);
    }
}
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mapper.AutoMapper.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
- 📘 [AutoMapper Documentation](https://docs.automapper.org)
