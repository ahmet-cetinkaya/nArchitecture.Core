using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstractions;
using NArchitecture.Core.Localization.Resource.Yaml;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using Shouldly;

namespace Core.Localization.Resource.Yaml.DependencyInjection.Tests;

public class ServiceCollectionResourceLocalizationManagerExtensionTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly IServiceCollection _services;

    public ServiceCollectionResourceLocalizationManagerExtensionTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testBasePath);
        _services = new ServiceCollection();

        // Mock Assembly.GetExecutingAssembly() by creating test directory structure
        string featuresPath = Path.Combine(_testBasePath, "Features");
        Directory.CreateDirectory(featuresPath);
    }

    [Fact(DisplayName = "Should register ResourceLocalizationManager when valid locale files exist")]
    public void AddYamlResourceLocalization_WithValidFiles_ShouldRegisterService()
    {
        // Arrange
        CreateTestFeatureWithLocales("Feature1", ["en", "tr"]);
        SetupAssemblyLocation();

        // Act
        _services.AddYamlResourceLocalization();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();

        localizationService.ShouldNotBe(null);
        localizationService!.GetType().ShouldBe(typeof(ResourceLocalizationManager));
    }

    [Fact(DisplayName = "Should handle empty Features directory")]
    public void AddYamlResourceLocalization_WithEmptyFeaturesDir_ShouldRegisterService()
    {
        // Arrange
        SetupAssemblyLocation();

        // Act
        _services.AddYamlResourceLocalization();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();

        localizationService.ShouldNotBe(null);
        localizationService!.GetType().ShouldBe(typeof(ResourceLocalizationManager));
    }

    [Theory(DisplayName = "Should handle various valid file name patterns")]
    [InlineData("index.en.yaml")]
    [InlineData("messages.tr.yaml")]
    [InlineData("errors.fr-FR.yaml")]
    public void AddYamlResourceLocalization_WithVariousFilePatterns_ShouldRegisterService(string fileName)
    {
        // Arrange
        CreateTestFeatureWithFile("TestFeature", fileName);
        SetupAssemblyLocation();

        // Act
        _services.AddYamlResourceLocalization();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();

        localizationService.ShouldNotBe(null);
        localizationService!.GetType().ShouldBe(typeof(ResourceLocalizationManager));
    }

    [Fact(DisplayName = "Should handle missing Locales directory")]
    public void AddYamlResourceLocalization_WithMissingLocalesDir_ShouldRegisterService()
    {
        // Arrange
        string featurePath = Path.Combine(_testBasePath, "Features", "Feature1");
        Directory.CreateDirectory(featurePath);
        SetupAssemblyLocation();

        // Act
        _services.AddYamlResourceLocalization();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();

        localizationService.ShouldNotBe(null);
        localizationService!.GetType().ShouldBe(typeof(ResourceLocalizationManager));
    }

    private void CreateTestFeatureWithLocales(string featureName, string[] locales)
    {
        string featurePath = Path.Combine(_testBasePath, "Features", featureName);
        string localesPath = Path.Combine(featurePath, "Resources", "Locales");
        Directory.CreateDirectory(localesPath);

        foreach (string locale in locales)
        {
            string content = $"test: Test {locale}";
            File.WriteAllText(Path.Combine(localesPath, $"index.{locale}.yaml"), content);
        }
    }

    private void CreateTestFeatureWithFile(string featureName, string fileName)
    {
        string featurePath = Path.Combine(_testBasePath, "Features", featureName);
        string localesPath = Path.Combine(featurePath, "Resources", "Locales");
        Directory.CreateDirectory(localesPath);

        string content = "test: Test Content";
        File.WriteAllText(Path.Combine(localesPath, fileName), content);
    }

    private void SetupAssemblyLocation()
    {
        // Use AppDomain's base directory for test path
        AppDomain.CurrentDomain.SetData("APPBASE", _testBasePath);
        Environment.SetEnvironmentVariable("APPBASE", _testBasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
            Directory.Delete(_testBasePath, true);
    }
}
