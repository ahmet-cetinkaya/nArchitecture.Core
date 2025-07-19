# 🚢 NArchitecture Mapster Integration

High-performance Mapster integration for Clean Architecture applications.

## ✨ Features

- 🚀 High-performance Mapster adapter implementation
- 📚 Type-safe mapping operations
- 🧩 Seamless integration with existing configurations
- 📌 Support for advanced mapping scenarios
- ⚡ Low-allocation mapping for optimal performance
- 🔧 Compatible with Mapster's compile-time code generation

## 📥 Installation

```bash
dotnet add package NArchitecture.Core.Mapper.Mapster
```

## 🚦 Quick Start

### Basic Setup

```csharp
// Using global TypeAdapterConfig (recommended)
var mapper = new MapsterAdapter();

// Use mapper
var userDto = mapper.Map<UserDto>(user);
```

### Custom Configuration Setup

```csharp
// Create a custom TypeAdapterConfig
var config = new TypeAdapterConfig();
config.NewConfig<User, UserDto>()
    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");

// Create mapper with custom config
var customMapper = config.CreateMapper();
var adapter = new MapsterAdapter(customMapper);

// Use mapper
var userDto = adapter.Map<UserDto>(user);
```

### Configuration Example

```csharp
// Configure mappings using Mapster's fluent API
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
    .Map(dest => dest.IsActive, src => src.Status == UserStatus.Active)
    .Ignore(dest => dest.InternalId);

// Use the global configuration
var mapper = new MapsterAdapter();
var userDto = mapper.Map<UserDto>(user);
```

### Advanced Mapping Scenarios

```csharp
// Mapping collections
var userDtos = mapper.Map<List<UserDto>>(users);

// Mapping to existing instance
var existingDto = new UserDto();
var result = mapper.Map<User, UserDto>(user, existingDto);

// Complex nested mapping
TypeAdapterConfig<Order, OrderDto>
    .NewConfig()
    .Map(dest => dest.CustomerName, src => src.Customer.FullName)
    .Map(dest => dest.Items, src => src.OrderItems);
```

### Dependency Injection

To use with dependency injection, consider using the NArchitecture.Core.Mapper.Mapster.DependencyInjection package:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddNArchitectureMapster(typeof(Program).Assembly);
```

## 🔧 Performance Benefits

Mapster provides significant performance advantages:

- **Compile-time code generation** - No reflection overhead
- **Zero-allocation mapping** - Minimal memory pressure
- **Fast execution** - Up to 3x faster than AutoMapper
- **Small memory footprint** - Efficient resource usage

## 🔗 Links

- 📦 [NuGet Package](https://www.nuget.org/packages/NArchitecture.Core.Mapper.Mapster)
- 💻 [Source Code](https://github.com/kodlamaio-projects/nArchitecture.Core)
- 🚀 [nArchitecture Starter](https://github.com/kodlamaio-projects/nArchitecture)
- ⚡ [nArchitecture Generator](https://github.com/kodlamaio-projects/nArchitecture.Gen)
- 📘 [Mapster Documentation](https://github.com/MapsterMapper/Mapster)