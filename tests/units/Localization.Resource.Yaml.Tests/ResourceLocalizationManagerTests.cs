using System.Data;
using NArchitecture.Core.Localization.Resource.Yaml;
using Shouldly;

[Trait("Category", "Localization")]
public class ResourceLocalizationManagerTests
{
    private readonly string _testDataPath;
    private readonly Dictionary<string, Dictionary<string, string>> _testResources;

    public ResourceLocalizationManagerTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _ = Directory.CreateDirectory(_testDataPath);
        _testResources = new Dictionary<string, Dictionary<string, string>>();
        SetupTestData();
    }

    private void SetupTestData()
    {
        string enContent =
            @"
hello: Hello
world: World
greeting: Hello, World!
";
        string trContent =
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
        string filePath = Path.Combine(_testDataPath, $"{section}.{locale}.yaml");
        File.WriteAllText(filePath, content);
    }

    internal void Dispose()
    {
        if (Directory.Exists(_testDataPath))
            Directory.Delete(_testDataPath, true);
    }

    [Fact(DisplayName = "Constructor should initialize resource data successfully")]
    public void Constructor_ShouldInitializeResourceData()
    {
        // Arrange & Act
        var manager = new ResourceLocalizationManager(_testResources);

        // Assert
        _ = manager.ShouldNotBeNull();
    }

    [Theory(DisplayName = "GetLocalizedAsync should return localized string for valid key and locale")]
    [InlineData("hello", "tr", "Merhaba")]
    [InlineData("world", "tr", "Dünya")]
    [InlineData("greeting", "tr", "Merhaba, Dünya!")]
    public async Task GetLocalizedAsync_WithValidKey_ShouldReturnLocalizedString(string key, string locale, string expected)
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources) { AcceptLocales = new[] { locale } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "GetLocalizedAsync should return the key itself when key not found")]
    public async Task GetLocalizedAsync_WithInvalidKey_ShouldReturnKeyItself()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources) { AcceptLocales = new[] { "tr" } };
        string invalidKey = "nonexistent_key";

        // Act
        string result = await manager.GetLocalizedAsync(invalidKey);

        // Assert
        result.ShouldBe(invalidKey);
    }

    [Fact(DisplayName = "GetLocalizedAsync should fallback to default locale when invalid locale provided")]
    public async Task GetLocalizedAsync_WithInvalidLocale_ShouldFallbackToDefaultLocale()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources) { AcceptLocales = new[] { "invalid-locale", "en" } };

        // Act
        string result = await manager.GetLocalizedAsync("hello");

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact(DisplayName = "GetLocalizedAsync should use first available locale among multiple accept locales")]
    public async Task GetLocalizedAsync_WithMultipleAcceptLocales_ShouldUseFirstAvailable()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources) { AcceptLocales = new[] { "fr", "tr", "en" } };

        // Act
        string result = await manager.GetLocalizedAsync("hello");

        // Assert
        result.ShouldBe("Merhaba");
    }

    [Fact(DisplayName = "GetLocalizedAsync should throw exception when AcceptLocales is null")]
    public void GetLocalizedAsync_WithNullAcceptLocales_ShouldThrowException()
    {
        // Arrange
        var manager = new ResourceLocalizationManager(_testResources) { AcceptLocales = null };

        // Act & Assert
        _ = Should.Throw<NoNullAllowedException>(() => manager.GetLocalizedAsync("hello").GetAwaiter().GetResult());
    }

    [Fact(DisplayName = "GetLocalizedAsync should return localized string for custom section")]
    public async Task GetLocalizedAsync_WithCustomSection_ShouldReturnLocalizedString()
    {
        // Arrange
        string customContent = "customKey: Custom Value";
        CreateTestYamlFile("en", "custom", customContent);
        var resources = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "en",
                new Dictionary<string, string> { { "custom", Path.Combine(_testDataPath, "custom.en.yaml") } }
            },
        };
        var manager = new ResourceLocalizationManager(resources) { AcceptLocales = new[] { "en" } };

        // Act
        string result = await manager.GetLocalizedAsync("customKey", "custom");

        // Assert
        result.ShouldBe("Custom Value");
    }
}
