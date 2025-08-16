using System.Data;
using System.Reflection;
using NArchitecture.Core.Localization.Abstractions;
using YamlDotNet.RepresentationModel;

namespace NArchitecture.Core.Localization.Resource.Yaml;

/// <summary>
/// Manages localization by reading YAML resource files.
/// </summary>
public class ResourceLocalizationManager : ILocalizationService
{
    private const string _defaultLocale = "en";
    private const string _defaultKeySection = "index";
    public ICollection<string>? AcceptLocales { get; set; }

    // Stores resource data per locale and section
    private readonly Dictionary<string, Dictionary<string, (string path, YamlMappingNode? content)>> _resourceData = [];

    public ResourceLocalizationManager(Dictionary<string, Dictionary<string, string>> resources)
    {
        foreach ((string locale, Dictionary<string, string> sectionResources) in resources)
        {
            if (!_resourceData.ContainsKey(locale))
                _resourceData.Add(locale, []);

            foreach ((string sectionName, string sectionResourcePath) in sectionResources)
                _resourceData[locale].Add(sectionName, (sectionResourcePath, null));
        }
    }

    /// <inheritdoc />
    public Task<string> GetLocalizedAsync(string key, string? keySection = null)
    {
        // Redirect to the overload that utilizes AcceptLocales.
        return GetLocalizedAsync(key, AcceptLocales ?? throw new NoNullAllowedException(nameof(AcceptLocales)), keySection);
    }

    /// <inheritdoc />
    public Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null)
    {
        string? localization;
        if (acceptLocales is not null)
        {
            // Attempt retrieving localization from provided locales.
            foreach (string locale in acceptLocales)
            {
                localization = GetLocalizationFromResource(key, locale, keySection);
                if (localization is not null)
                    return Task.FromResult(localization);
            }
        }

        localization = GetLocalizationFromResource(key, _defaultLocale, keySection);
        if (localization is not null)
            return Task.FromResult(localization);

        return Task.FromResult(key);
    }

    /// <summary>
    /// Retrieves the localized string from resource data for a given key and locale.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="locale">The locale identifier.</param>
    /// <param name="keySection">The section of resources to use. Defaults to index if not provided.</param>
    /// <returns>The localized string if found; otherwise, null.</returns>
    private string? GetLocalizationFromResource(string key, string locale, string? keySection = _defaultKeySection)
    {
        if (string.IsNullOrWhiteSpace(keySection))
            keySection = _defaultKeySection;

        if (
            _resourceData.TryGetValue(locale, out Dictionary<string, (string path, YamlMappingNode? content)>? cultureNode)
            && cultureNode.TryGetValue(keySection, out (string path, YamlMappingNode? content) sectionNode)
        )
        {
            // Lazy-load YAML content if not loaded yet
            if (sectionNode.content is null)
            {
                if (sectionNode.path.StartsWith("embedded:"))
                    LazyLoadEmbeddedResource(sectionNode.path, out sectionNode.content);
                else
                    LazyLoadFileResource(sectionNode.path, out sectionNode.content);
            }

            if (sectionNode.content!.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? cultureValueNode))
                return cultureValueNode.ToString();
        }

        return null;
    }

    /// <summary>
    /// Loads YAML resource from file and outputs its root mapping node.
    /// </summary>
    /// <param name="path">The file path to the YAML resource.</param>
    /// <param name="content">The loaded YAML mapping node.</param>
    private static void LazyLoadFileResource(string path, out YamlMappingNode? content)
    {
        // Open and parse the YAML file
        using StreamReader reader = new(path);
        YamlStream yamlStream = [];
        yamlStream.Load(reader);
        content = (YamlMappingNode)yamlStream.Documents[0].RootNode;
    }

    /// <summary>
    /// Loads YAML resource from embedded resource and outputs its root mapping node.
    /// </summary>
    /// <param name="embeddedPath">The embedded resource path (prefixed with "embedded:").</param>
    /// <param name="content">The loaded YAML mapping node.</param>
    private static void LazyLoadEmbeddedResource(string embeddedPath, out YamlMappingNode? content)
    {
        content = null;
        
        // Remove "embedded:" prefix
        var resourceName = embeddedPath[9..];
        
        // Find the assembly that contains this resource
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            if (resourceNames.Contains(resourceName))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    YamlStream yamlStream = [];
                    yamlStream.Load(reader);
                    content = (YamlMappingNode)yamlStream.Documents[0].RootNode;
                    return;
                }
            }
        }
    }
}
