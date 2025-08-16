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
    ///        <description>First tries to load embedded resources with pattern "Features.{featureName}.Resources.Locales.{uniqueKeySectionName}.{culture}.yaml"</description>
    ///    </item>
    ///    <item>
    ///        <description>Falls back to file system resources in "{assembly}/Features/{featureName}/Resources/Locales/"</description>
    ///    </item>
    ///    <item>
    ///        <description>YAML file names must be like {uniqueKeySectionName}.{culture}.yaml</description>
    ///    </item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="assembliesToScan">The assemblies to scan for localization resources.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        _ = services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            Dictionary<string, Dictionary<string, string>> resources = GetLocalizationResources(assembliesToScan);
            return new ResourceLocalizationManager(resources);
        });

        return services;
    }

    private static Dictionary<string, Dictionary<string, string>> GetLocalizationResources(Assembly[] assembliesToScan)
    {
        var resources = new Dictionary<string, Dictionary<string, string>>();
        
        foreach (var assembly in assembliesToScan)
        {
            // First try embedded resources
            GetEmbeddedResources(assembly, resources);
            
            // Then try file system resources as fallback
            GetFileSystemResources(assembly, resources);
        }

        return resources;
    }

    private static void GetEmbeddedResources(Assembly assembly, Dictionary<string, Dictionary<string, string>> resources)
    {
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains($"{FeaturesFolder}.") && 
                          name.Contains($"{ResourcesFolder}.{LocalesFolder}.") && 
                          name.EndsWith(YamlExtension))
            .ToArray();

        foreach (var resourceName in resourceNames)
        {
            // Parse: AssemblyName.Features.FeatureName.Resources.Locales.filename.culture.yaml
            var parts = resourceName.Split('.');
            if (parts.Length < 6) continue;

            var featureIndex = Array.FindIndex(parts, p => p == FeaturesFolder);
            if (featureIndex == -1 || featureIndex + 4 >= parts.Length) continue;

            var featureName = parts[featureIndex + 1];
            var fileName = string.Join('.', parts[(featureIndex + 4)..^2]); // Remove .yaml
            var lastDotIndex = fileName.LastIndexOf('.');
            if (lastDotIndex == -1) continue;

            var culture = fileName[(lastDotIndex + 1)..];

            if (!resources.ContainsKey(culture))
                resources[culture] = [];

            // Store the embedded resource name instead of file path
            resources[culture][featureName] = $"embedded:{resourceName}";
        }
    }

    private static void GetFileSystemResources(Assembly assembly, Dictionary<string, Dictionary<string, string>> resources)
    {
        string? assemblyLocation = Path.GetDirectoryName(assembly.Location);
        if (assemblyLocation is null)
            return;

        string featuresPath = Path.Combine(assemblyLocation, FeaturesFolder);
        if (!Directory.Exists(featuresPath))
            return;

        string[] featureDirectories = Directory.GetDirectories(featuresPath);

        foreach (string featureDir in featureDirectories)
        {
            var featureName = Path.GetFileName(featureDir);
            IEnumerable<(string culture, string filePath)> localeFiles = GetLocaleFiles(featureDir);
            
            foreach ((string culture, string filePath) in localeFiles)
            {
                if (!resources.ContainsKey(culture))
                    resources[culture] = [];

                // Only add if not already present from embedded resources
                if (!resources[culture].ContainsKey(featureName))
                {
                    resources[culture][featureName] = filePath;
                }
            }
        }
    }

    private static IEnumerable<(string culture, string filePath)> GetLocaleFiles(string featureDir)
    {
        string localeDir = Path.Combine(featureDir, ResourcesFolder, LocalesFolder);
        if (!Directory.Exists(localeDir))
            yield break;

        foreach (string file in Directory.EnumerateFiles(localeDir, FileSearchPattern))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            int separatorIndex = fileName.LastIndexOf('.'); // Use LastIndexOf for better parsing
            if (separatorIndex == -1)
                continue;

            string culture = fileName[(separatorIndex + 1)..];
            yield return (culture, file);
        }
    }
}
