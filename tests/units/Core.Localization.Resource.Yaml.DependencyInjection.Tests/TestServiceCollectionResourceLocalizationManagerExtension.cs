using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstractions;
using NArchitecture.Core.Localization.Resource.Yaml;

namespace Core.Localization.Resource.Yaml.DependencyInjection.Tests;

public static class TestServiceCollectionResourceLocalizationManagerExtension
{
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
    {
        var basePath = GetTestBasePath();
        _ = services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            var resources = GetLocalizationResources(basePath);
            return new ResourceLocalizationManager(resources);
        });

        return services;
    }

    private static string GetTestBasePath() =>
        AppDomain.CurrentDomain.GetData("APPBASE")?.ToString()
        ?? Environment.GetEnvironmentVariable("APPBASE")
        ?? throw new InvalidOperationException("Test base path not set");

    private static Dictionary<string, Dictionary<string, string>> GetLocalizationResources(string basePath)
    {
        var resources = new Dictionary<string, Dictionary<string, string>>();
        var featuresPath = Path.Combine(basePath, "Features");
        if (!Directory.Exists(featuresPath))
            return resources;

        foreach (var featureDir in Directory.GetDirectories(featuresPath))
        {
            var localeFiles = GetLocaleFiles(featureDir);
            foreach (var (culture, filePath) in localeFiles)
            {
                _ = resources.TryAdd(culture, new Dictionary<string, string>());
                resources[culture][Path.GetFileName(featureDir)] = filePath;
            }
        }

        return resources;
    }

    private static IEnumerable<(string culture, string filePath)> GetLocaleFiles(string featureDir)
    {
        var localeDir = Path.Combine(featureDir, "Resources", "Locales");
        if (!Directory.Exists(localeDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(localeDir, "*.yaml"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var separatorIndex = fileName.IndexOf('.');
            if (separatorIndex == -1)
                continue;

            var culture = fileName[(separatorIndex + 1)..];
            yield return (culture, file);
        }
    }
}
