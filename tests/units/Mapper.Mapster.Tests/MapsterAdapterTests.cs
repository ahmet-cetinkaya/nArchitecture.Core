using Mapster;
using Moq;
using NArchitecture.Core.Mapper.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Mapper.Mapster.Tests;

[Trait("Category", "Unit")]
public class MapsterAdapterTests
{
    private readonly TypeAdapterConfig _config;
    private readonly MapsterAdapter _adapter;

    public MapsterAdapterTests()
    {
        _config = new TypeAdapterConfig();
        _adapter = new MapsterAdapter(_config);
    }

    [Fact(DisplayName = "Map should return mapped destination object when source is valid")]
    public void Map_ShouldReturnMappedDestinationObject_WhenSourceIsValid()
    {
        // Arrange: Create a source object with test data.
        var source = new TestSourceClass { Name = "Test Name", Age = 25 };

        // Act: Map source to destination.
        TestDestinationClass result = _adapter.Map<TestDestinationClass>(source);

        // Assert: Verify the mapping was successful.
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Name");
        result.Age.ShouldBe(25);
    }

    [Fact(DisplayName = "Map should return mapped destination object with source and destination types")]
    public void Map_ShouldReturnMappedDestinationObject_WithSourceAndDestinationTypes()
    {
        // Arrange: Create a source object with test data.
        var source = new TestSourceClass { Name = "Test Name", Age = 30 };

        // Act: Map source to destination using generic overload.
        TestDestinationClass result = _adapter.Map<TestSourceClass, TestDestinationClass>(source);

        // Assert: Verify the mapping was successful.
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Name");
        result.Age.ShouldBe(30);
    }

    [Fact(DisplayName = "Map should map to existing destination object")]
    public void Map_ShouldMapToExistingDestinationObject()
    {
        // Arrange: Create source and existing destination objects.
        var source = new TestSourceClass { Name = "Updated Name", Age = 35 };
        var destination = new TestDestinationClass { Name = "Original Name", Age = 20 };

        // Act: Map source to existing destination.
        TestDestinationClass result = _adapter.Map(source, destination);

        // Assert: Verify the existing object was updated.
        result.ShouldBeSameAs(destination);
        result.Name.ShouldBe("Updated Name");
        result.Age.ShouldBe(35);
    }

    [Fact(DisplayName = "Map should map to existing destination object with generic types")]
    public void Map_ShouldMapToExistingDestinationObject_WithGenericTypes()
    {
        // Arrange: Create source and existing destination objects.
        var source = new TestSourceClass { Name = "Generic Update", Age = 40 };
        var destination = new TestDestinationClass { Name = "Original", Age = 10 };

        // Act: Map source to existing destination using generic overload.
        TestDestinationClass result = _adapter.Map<TestSourceClass, TestDestinationClass>(source, destination);

        // Assert: Verify the existing object was updated.
        result.ShouldBeSameAs(destination);
        result.Name.ShouldBe("Generic Update");
        result.Age.ShouldBe(40);
    }

    [Fact(DisplayName = "Map should handle null source gracefully")]
    public void Map_ShouldHandleNullSource_Gracefully()
    {
        // Act & Assert: Verify null source returns null.
        TestDestinationClass result = _adapter.Map<TestDestinationClass>(null!);
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Map should handle null source with generic types gracefully")]
    public void Map_ShouldHandleNullSourceWithGenericTypes_Gracefully()
    {
        // Act & Assert: Verify null source returns null.
        TestDestinationClass result = _adapter.Map<TestSourceClass, TestDestinationClass>(null!);
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Map should handle null destination when mapping to existing object")]
    public void Map_ShouldHandleNullDestination_WhenMappingToExistingObject()
    {
        // Arrange: Create source object.
        var source = new TestSourceClass { Name = "Test", Age = 25 };

        // Act: Map to null destination.
        TestDestinationClass result = _adapter.Map(source, (TestDestinationClass?)null);

        // Assert: Verify new object is created.
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Age.ShouldBe(25);
    }

    [Fact(DisplayName = "Map should use custom configuration when provided")]
    public void Map_ShouldUseCustomConfiguration_WhenProvided()
    {
        // Arrange: Configure custom mapping rule.
        var customConfig = new TypeAdapterConfig();
        customConfig.NewConfig<TestSourceClass, TestDestinationClass>().Map(dest => dest.Name, src => $"Prefix_{src.Name}");

        var customAdapter = new MapsterAdapter(customConfig);
        var source = new TestSourceClass { Name = "Test", Age = 25 };

        // Act: Map using custom configuration.
        TestDestinationClass result = customAdapter.Map<TestDestinationClass>(source);

        // Assert: Verify custom mapping was applied.
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Prefix_Test");
        result.Age.ShouldBe(25);
    }

    [Fact(DisplayName = "Map should handle complex object mapping")]
    public void Map_ShouldHandleComplexObjectMapping()
    {
        // Arrange: Create complex source object.
        var source = new ComplexSourceClass
        {
            Id = 1,
            Name = "Complex Test",
            NestedObject = new NestedClass { Value = "Nested Value" },
            Items = new List<string> { "Item1", "Item2" },
        };

        // Act: Map complex object.
        ComplexDestinationClass result = _adapter.Map<ComplexDestinationClass>(source);

        // Assert: Verify complex mapping was successful.
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Complex Test");
        result.NestedObject.ShouldNotBeNull();
        result.NestedObject.Value.ShouldBe("Nested Value");
        result.Items.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldContain("Item1");
        result.Items.ShouldContain("Item2");
    }

    [Fact(DisplayName = "Constructor should throw when config is null")]
    public void Constructor_ShouldThrow_WhenConfigIsNull()
    {
        // Act & Assert: Verify constructor throws when passed null config.
        Should.Throw<ArgumentNullException>(() => new MapsterAdapter(null!));
    }
}

// Test classes for mapping
public class TestSourceClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

public class TestDestinationClass
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

public class ComplexSourceClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public NestedClass? NestedObject { get; set; }
    public List<string>? Items { get; set; }
}

public class ComplexDestinationClass
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public NestedClass? NestedObject { get; set; }
    public List<string>? Items { get; set; }
}

public class NestedClass
{
    public string? Value { get; set; }
}
