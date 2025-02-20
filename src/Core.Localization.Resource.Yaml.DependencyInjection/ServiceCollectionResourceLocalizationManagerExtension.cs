using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstractions;

namespace NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;

public static class ServiceCollectionResourceLocalizationManagerExtension
{
    // Define constants to avoid magic strings
    private const string FeaturesFolder = "Features";
    private const string ResourcesFolder = "Resources";
    private const string LocalesFolder = "Locales";
    private const string YamlExtension = ".yaml";
    private const string FileSearchPattern = "*" + YamlExtension;

    /// <summary>
    /// Adds <see cref="ResourceLocalizationManager"/> as <see cref="ILocalizationService"/> to <see cref="IServiceCollection"/>.
    /// <list type="bullet">
    ///    <item>
    ///        <description>Reads all yaml files in the "<see cref="Assembly.GetExecutingAssembly()"/>/Features/{featureName}/Resources/Locales/". Yaml file names must be like {uniqueKeySectionName}.{culture}.yaml.</description>
    ///    </item>
    ///    <item>
    ///        <description>If you don't want separate locale files with sections, create "<see cref="Assembly.GetExecutingAssembly()"/>/Features/Index/Resources/Locales/index.{culture}.yaml".</description>
    ///    </item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
    {
        services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            Dictionary<string, Dictionary<string, string>> resources = GetLocalizationResources();
            return new ResourceLocalizationManager(resources);
        });

        return services;
    }

    private static Dictionary<string, Dictionary<string, string>> GetLocalizationResources()
    {
        var resources = new Dictionary<string, Dictionary<string, string>>();
        string? assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyLocation is null)
            return resources;

        string featuresPath = Path.Combine(assemblyLocation, FeaturesFolder);
        if (!Directory.Exists(featuresPath))
            return resources;

        string[] featureDirectories = Directory.GetDirectories(featuresPath);

        foreach (string featureDir in featureDirectories)
        {
            IEnumerable<(string culture, string filePath)> localeFiles = GetLocaleFiles(featureDir);
            foreach ((string culture, string filePath) in localeFiles)
            {
                if (!resources.ContainsKey(culture))
                    resources[culture] = [];

                resources[culture][Path.GetFileName(featureDir)] = filePath;
            }
        }

        return resources;
    }

    private static IEnumerable<(string culture, string filePath)> GetLocaleFiles(string featureDir)
    {
        string localeDir = Path.Combine(featureDir, ResourcesFolder, LocalesFolder);
        if (!Directory.Exists(localeDir))
            yield break;

        foreach (string file in Directory.EnumerateFiles(localeDir, FileSearchPattern))
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
