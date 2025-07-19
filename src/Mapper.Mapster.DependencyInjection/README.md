# 🔌 NArchitecture Mapster Dependency Injection

Dependency Injection extension methods for seamless integration of Mapster in Clean Architecture applications.

## ✨ Features

- 🚀 Simple service registration
- 📦 Profile scanning using marker interfaces
- 🔄 Automatic adapter registration
- 🔗 Clean integration with Microsoft DI
- 🌐 Supports Mapster's IRegister interface
- ⚡ High-performance mapping with compile-time code generation
- 🎯 Global and scoped configuration options

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mapper.Mapster.DependencyInjection
```

## 🚦 Quick Start

### Register Profiles Using Marker Interface

The recommended way is to mark your profile classes with `IMappingProfile` interface and implement `IRegister`:

```csharp
using Mapster;
using NArchitecture.Core.Mapper.Abstractions;

// Mark your profile with the interface and implement IRegister
public class UserMappingProfile : IRegister, IMappingProfile<UserMappingProfile>
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    }
}
```

Then register them:

```csharp
using NArchitecture.Core.Mapper.Mapster.DependencyInjection;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register only profiles that implement IMappingProfile
builder.Services.AddNArchitectureMapster(
    typeof(Program).Assembly, 
    typeof(UserMappingProfile).Assembly
);
```

### Register All Profiles Without Marker Interface

If you prefer to register all IRegister implementations without using the marker interface:

```csharp
// Register all IRegister implementations without filtering
builder.Services.AddNArchitectureMapster(
    configAction: null,
    ServiceLifetime.Singleton,
    filterByInterface: false,
    typeof(Program).Assembly
);
```

### Using Global Configuration

For simple scenarios, you can use the global TypeAdapterConfig:

```csharp
// Configure global settings and register adapter
builder.Services.AddNArchitectureMapsterGlobal(config =>
{
    config.NewConfig<User, UserDto>()
        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
});
```

## 📝 Examples

### Adding Custom Mapster Configuration

```csharp
// Register with custom configuration
builder.Services.AddNArchitectureMapster(
    config => 
    {
        // Custom Mapster configuration here
        config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
        config.Default.PreserveReference(true);
    },
    typeof(Program).Assembly
);
```

### Using Different Service Lifetime

```csharp
// Register with transient lifetime
builder.Services.AddNArchitectureMapster(
    ServiceLifetime.Transient,
    typeof(Program).Assembly
);
```

### Complex Profile Example

```csharp
public class OrderMappingProfile : IRegister, IMappingProfile<OrderMappingProfile>
{
    public void Register(TypeAdapterConfig config)
    {
        // Basic mapping
        config.NewConfig<Order, OrderDto>();

        // Complex mapping with custom logic
        config.NewConfig<Order, OrderSummaryDto>()
            .Map(dest => dest.CustomerName, src => src.Customer.FullName)
            .Map(dest => dest.TotalAmount, src => src.Items.Sum(i => i.Price * i.Quantity))
            .Map(dest => dest.ItemCount, src => src.Items.Count)
            .Ignore(dest => dest.InternalNotes);

        // Conditional mapping
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.DisplayName, 
                 src => !string.IsNullOrEmpty(src.NickName) ? src.NickName : src.FullName);
    }
}
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
        
        // Use the high-performance mapper adapter
        return _mapper.Map<UserDto>(user);
    }
    
    public List<UserDto> GetAllUsers()
    {
        var users = _repository.GetAll();
        
        // Efficient collection mapping
        return _mapper.Map<List<UserDto>>(users);
    }
}
```

### Advanced Configuration Scenarios

```csharp
// Multiple assemblies with custom configuration
builder.Services.AddNArchitectureMapster(
    config =>
    {
        // Global settings
        config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
        config.RequireDestinationMemberSource = true;
        
        // Custom type adapters
        config.ForType<DateTime, string>()
            .MapWith(src => src.ToString("yyyy-MM-dd"));
            
        // Conditional mapping
        config.When((srcType, destType, mapType) => srcType == destType)
            .Ignore("Id", "CreatedAt", "UpdatedAt");
    },
    ServiceLifetime.Scoped,
    filterByInterface: true,
    typeof(Program).Assembly,
    typeof(Domain.User).Assembly,
    typeof(Application.UserDto).Assembly
);
```

## 🚀 Performance Benefits

Mapster provides exceptional performance advantages:

- **Compile-time code generation** - Zero reflection overhead
- **Minimal allocations** - Efficient memory usage
- **Fast execution** - Superior performance compared to other mappers
- **Small footprint** - Lightweight runtime impact

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mapper.Mapster.DependencyInjection)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
- 📘 [Mapster Documentation](https://github.com/MapsterMapper/Mapster)