using System.Data;
using Shouldly;

namespace NArchitecture.Core.Localization.Resource.Yaml.Tests;

public class ResourceLocalizationManagerTests
{
    private readonly string _testDataPath;
    private readonly Dictionary<string, Dictionary<string, string>> _testResources;

    public ResourceLocalizationManagerTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testDataPath);
        _testResources = new Dictionary<string, Dictionary<string, string>>();
        SetupTestData();
    }

    private void SetupTestData()
    {
        var enContent =
            @"
hello: Hello
world: World
greeting: Hello, World!
";
        var trContent =
            @"
hello: Merhaba
world: Dünya
greeting: Merhaba, Dünya!
";

        CreateTestYamlFile("en", "index", enContent);
        CreateTestYamlFile("tr", "index", trContent);

        _testResources.Add("en", new Dictionary<string, string> { { "index", Path.Combine(_testDataPath, "index.en.yaml") } });
        _testResources.Add("tr", new Dictionary<string, string> { { "index", Path.Combine(_testDataPath, "index.tr.yaml") } });
    }

    private void CreateTestYamlFile(string locale, string section, string content)
    {
        var filePath = Path.Combine(_testDataPath, $"{section}.{locale}.yaml");
        File.WriteAllText(filePath, content);
    }

    internal void Dispose()
    {
        if (Directory.Exists(_testDataPath))
            Directory.Delete(_testDataPath, true);
    }

    [Fact]
    public void Constructor_ShouldInitializeResourceData()
    {
        // Arrange & Act
        var manager = new ResourceLocalizationManager(_testResources);

        // Assert
        manager.ShouldNotBe(null);
    }

    [Theory]
    [InlineData("hello", "tr", "Merhaba")]
    [InlineData("world", "tr", "Dünya")]
    [InlineData("greeting", "tr", "Merhaba, Dünya!")]
    public async Task GetLocalizedAsync_WithValidKey_ShouldReturnLocalizedString(string key, string locale, string expected)
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources);
        manager.AcceptLocales = new[] { locale };

        // Act
        var result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetLocalizedAsync_WithInvalidKey_ShouldReturnKeyItself()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources);
        manager.AcceptLocales = new[] { "tr" };
        var invalidKey = "nonexistent_key";

        // Act
        var result = await manager.GetLocalizedAsync(invalidKey);

        // Assert
        result.ShouldBe(invalidKey);
    }

    [Fact]
    public async Task GetLocalizedAsync_WithInvalidLocale_ShouldFallbackToDefaultLocale()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources);
        manager.AcceptLocales = new[] { "invalid-locale", "en" };

        // Act
        var result = await manager.GetLocalizedAsync("hello");

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact]
    public async Task GetLocalizedAsync_WithMultipleAcceptLocales_ShouldUseFirstAvailable()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources);
        manager.AcceptLocales = new[] { "fr", "tr", "en" };

        // Act
        var result = await manager.GetLocalizedAsync("hello");

        // Assert
        result.ShouldBe("Merhaba");
    }

    [Fact]
    public void GetLocalizedAsync_WithNullAcceptLocales_ShouldThrowException()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources);
        manager.AcceptLocales = null;

        // Act & Assert
        Should.Throw<NoNullAllowedException>(() => manager.GetLocalizedAsync("hello").GetAwaiter().GetResult());
    }

    [Fact]
    public async Task GetLocalizedAsync_WithCustomSection_ShouldReturnLocalizedString()
    {
        // Arrange
        var customContent = "customKey: Custom Value";
        CreateTestYamlFile("en", "custom", customContent);
        var resources = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "en",
                new Dictionary<string, string> { { "custom", Path.Combine(_testDataPath, "custom.en.yaml") } }
            },
        };
        var manager = new ResourceLocalizationManager(resources);
        manager.AcceptLocales = new[] { "en" };

        // Act
        var result = await manager.GetLocalizedAsync("customKey", "custom");

        // Assert
        result.ShouldBe("Custom Value");
    }
}
