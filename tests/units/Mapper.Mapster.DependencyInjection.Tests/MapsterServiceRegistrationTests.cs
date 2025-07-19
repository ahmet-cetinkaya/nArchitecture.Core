using System.Reflection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NArchitecture.Core.Mapper.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Mapper.Mapster.DependencyInjection.Tests;

[Trait("Category", "Unit")]
public class MapsterServiceRegistrationTests
{
    [Fact(DisplayName = "AddNArchitectureMapster should register IMapper as singleton")]
    public void AddNArchitectureMapster_ShouldRegisterIMapperAsSingleton()
    {
        // Arrange: Create service collection.
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act: Add NArchitecture Mapster services.
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify IMapper is registered as singleton.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper1 = serviceProvider.GetService<IMapper>();
        var mapper2 = serviceProvider.GetService<IMapper>();

        mapper1.ShouldNotBeNull();
        mapper2.ShouldNotBeNull();
        mapper1.ShouldBeSameAs(mapper2); // Singleton check
        mapper1.ShouldBeOfType<MapsterAdapter>();
    }

    [Fact(DisplayName = "AddNArchitectureMapster should register TypeAdapterConfig as singleton")]
    public void AddNArchitectureMapster_ShouldRegisterTypeAdapterConfigAsSingleton()
    {
        // Arrange: Create service collection.
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act: Add NArchitecture Mapster services.
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify TypeAdapterConfig is registered as singleton.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var config1 = serviceProvider.GetService<TypeAdapterConfig>();
        var config2 = serviceProvider.GetService<TypeAdapterConfig>();

        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config1.ShouldBeSameAs(config2); // Singleton check
    }

    [Fact(DisplayName = "AddNArchitectureMapster should discover and apply mapping profiles")]
    public void AddNArchitectureMapster_ShouldDiscoverAndApplyMappingProfiles()
    {
        // Arrange: Create service collection.
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act: Add NArchitecture Mapster services.
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify mapping profiles are applied.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetService<IMapper>();

        mapper.ShouldNotBeNull();

        // Test that the profile configuration is applied
        var source = new TestProfileSource { Name = "Test", Value = 42 };
        var result = mapper.Map<TestProfileDestination>(source);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(42);
    }

    [Fact(DisplayName = "AddNArchitectureMapster should handle multiple assemblies")]
    public void AddNArchitectureMapster_ShouldHandleMultipleAssemblies()
    {
        // Arrange: Create service collection and multiple assemblies.
        var services = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(MapsterAdapter).Assembly };

        // Act: Add NArchitecture Mapster services with multiple assemblies.
        services.AddNArchitectureMapster(assemblies);

        // Assert: Verify services are registered correctly.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetService<IMapper>();
        var config = serviceProvider.GetService<TypeAdapterConfig>();

        mapper.ShouldNotBeNull();
        config.ShouldNotBeNull();
        mapper.ShouldBeOfType<MapsterAdapter>();
    }

    [Fact(DisplayName = "AddNArchitectureMapster should handle empty assembly array")]
    public void AddNArchitectureMapster_ShouldHandleEmptyAssemblyArray()
    {
        // Arrange: Create service collection with empty assembly array.
        var services = new ServiceCollection();
        var assemblies = Array.Empty<Assembly>();

        // Act: Add NArchitecture Mapster services with empty assemblies.
        services.AddNArchitectureMapster(assemblies);

        // Assert: Verify basic services are still registered.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetService<IMapper>();
        var config = serviceProvider.GetService<TypeAdapterConfig>();

        mapper.ShouldNotBeNull();
        config.ShouldNotBeNull();
        mapper.ShouldBeOfType<MapsterAdapter>();
    }

    [Fact(DisplayName = "AddNArchitectureMapster should throw when services is null")]
    public void AddNArchitectureMapster_ShouldThrow_WhenServicesIsNull()
    {
        // Arrange: Null service collection.
        IServiceCollection services = null!;
        var assembly = Assembly.GetExecutingAssembly();

        // Act & Assert: Verify ArgumentNullException is thrown.
        Should.Throw<ArgumentNullException>(() => services.AddNArchitectureMapster(assembly));
    }

    [Fact(DisplayName = "AddNArchitectureMapster should throw when assembly is null")]
    public void AddNArchitectureMapster_ShouldThrow_WhenAssemblyIsNull()
    {
        // Arrange: Service collection with null assembly.
        var services = new ServiceCollection();
        Assembly assembly = null!;

        // Act & Assert: Verify ArgumentNullException is thrown.
        Should.Throw<ArgumentNullException>(() => services.AddNArchitectureMapster(assembly));
    }

    [Fact(DisplayName = "AddNArchitectureMapster should throw when assemblies array is null")]
    public void AddNArchitectureMapster_ShouldThrow_WhenAssembliesArrayIsNull()
    {
        // Arrange: Service collection with null assemblies array.
        var services = new ServiceCollection();
        Assembly[] assemblies = null!;

        // Act & Assert: Verify ArgumentNullException is thrown.
        Should.Throw<ArgumentNullException>(() => services.AddNArchitectureMapster(assemblies));
    }

    [Fact(DisplayName = "AddNArchitectureMapster should handle assemblies with no profiles")]
    public void AddNArchitectureMapster_ShouldHandleAssembliesWithNoProfiles()
    {
        // Arrange: Create service collection with assembly that has no profiles.
        var services = new ServiceCollection();
        var assembly = typeof(string).Assembly; // System assembly with no mapping profiles

        // Act: Add NArchitecture Mapster services.
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify services are still registered correctly.
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetService<IMapper>();
        var config = serviceProvider.GetService<TypeAdapterConfig>();

        mapper.ShouldNotBeNull();
        config.ShouldNotBeNull();
        mapper.ShouldBeOfType<MapsterAdapter>();
    }

    [Fact(DisplayName = "AddNArchitectureMapster should register services with correct lifetimes")]
    public void AddNArchitectureMapster_ShouldRegisterServicesWithCorrectLifetimes()
    {
        // Arrange: Create service collection.
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act: Add NArchitecture Mapster services.
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify service lifetimes.
        var mapperDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMapper));
        var configDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TypeAdapterConfig));

        mapperDescriptor.ShouldNotBeNull();
        mapperDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        configDescriptor.ShouldNotBeNull();
        configDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact(DisplayName = "AddNArchitectureMapster should allow multiple registrations without conflicts")]
    public void AddNArchitectureMapster_ShouldAllowMultipleRegistrationsWithoutConflicts()
    {
        // Arrange: Create service collection.
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act: Add NArchitecture Mapster services multiple times.
        services.AddNArchitectureMapster(assembly);
        services.AddNArchitectureMapster(assembly);

        // Assert: Verify services are registered correctly (last registration wins).
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var mapper = serviceProvider.GetService<IMapper>();
        var config = serviceProvider.GetService<TypeAdapterConfig>();

        mapper.ShouldNotBeNull();
        config.ShouldNotBeNull();
        mapper.ShouldBeOfType<MapsterAdapter>();
    }
}

// Test classes for mapping profile tests
public class TestProfileSource
{
    public string? Name { get; set; }
    public int Value { get; set; }
}

public class TestProfileDestination
{
    public string? Name { get; set; }
    public int Value { get; set; }
}

// Test mapping profile
public class TestMappingProfile : IMappingProfile<TestProfileSource>
{
    public void Configure(TypeAdapterConfig config)
    {
        config
            .NewConfig<TestProfileSource, TestProfileDestination>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Value, src => src.Value);
    }
}
