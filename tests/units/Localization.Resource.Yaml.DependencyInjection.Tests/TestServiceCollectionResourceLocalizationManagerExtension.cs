using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstractions;

namespace NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection.Tests;

public static class TestServiceCollectionResourceLocalizationManagerExtension
{
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
    {
        string basePath = GetTestBasePath();
        _ = services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            Dictionary<string, Dictionary<string, string>> resources = GetLocalizationResources(basePath);
            return new ResourceLocalizationManager(resources);
        });

        return services;
    }

    private static string GetTestBasePath()
    {
        return AppDomain.CurrentDomain.GetData("APPBASE")?.ToString()
            ?? Environment.GetEnvironmentVariable("APPBASE")
            ?? throw new InvalidOperationException("Test base path not set");
    }

    private static Dictionary<string, Dictionary<string, string>> GetLocalizationResources(string basePath)
    {
        var resources = new Dictionary<string, Dictionary<string, string>>();
        string featuresPath = Path.Combine(basePath, "Features");
        if (!Directory.Exists(featuresPath))
            return resources;

        foreach (string featureDir in Directory.GetDirectories(featuresPath))
        {
            IEnumerable<(string culture, string filePath)> localeFiles = GetLocaleFiles(featureDir);
            foreach ((string culture, string filePath) in localeFiles)
            {
                _ = resources.TryAdd(culture, []);
                resources[culture][Path.GetFileName(featureDir)] = filePath;
            }
        }

        return resources;
    }

    private static IEnumerable<(string culture, string filePath)> GetLocaleFiles(string featureDir)
    {
        string localeDir = Path.Combine(featureDir, "Resources", "Locales");
        if (!Directory.Exists(localeDir))
            yield break;

        foreach (string file in Directory.EnumerateFiles(localeDir, "*.yaml"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            int separatorIndex = fileName.IndexOf('.');
            if (separatorIndex == -1)
                continue;

            string culture = fileName[(separatorIndex + 1)..];
            yield return (culture, file);
        }
    }
}
