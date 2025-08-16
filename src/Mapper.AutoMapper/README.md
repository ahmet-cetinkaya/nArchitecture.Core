# 🚢 NArchitecture AutoMapper Integration

AutoMapper integration for Clean Architecture applications.

## ✨ Features

- 🔄 Clean AutoMapper adapter implementation
- 📚 Type-safe mapping operations
- 🧩 Seamless integration with existing profiles
- 📌 Support for advanced mapping scenarios
- 🛠️ Performance-optimized implementation

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mapper.AutoMapper
```

## 🚦 Quick Start

### Basic Setup

```csharp
// Create an AutoMapper configuration
var configuration = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<UserMappingProfile>();
});

// Create the AutoMapper and our adapter
var autoMapper = configuration.CreateMapper();
var mapper = new AutoMapperAdapter(autoMapper);

// Use mapper
var userDto = mapper.Map<UserDto>(user);
```

### Configuration Example

```csharp
// Create your AutoMapper profile
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => 
                opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
```

### Dependency Injection

To use with dependency injection, consider using the NArchitecture.Core.Mapper.AutoMapper.DependencyInjection package:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddSingleton<NArchitecture.Core.Mapper.Abstractions.IMapper, AutoMapperAdapter>();
```

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mapper.AutoMapper)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
- 📘 [AutoMapper Documentation](https://docs.automapper.org)
