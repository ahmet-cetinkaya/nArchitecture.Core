using System.Data;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Translation.Abstraction;

namespace NArchitecture.Core.Localization.Translation;

/// <summary>
/// Implements localization by leveraging a translation service to retrieve localized strings.
/// </summary>
public class TranslateLocalizationManager : ILocalizationService
{
    private const string _defaultLocale = "en";
    public ICollection<string>? AcceptLocales { get; set; }

    private readonly ITranslationService _translationService;

    public TranslateLocalizationManager(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    /// <inheritdoc />
    public Task<string> GetLocalizedAsync(string key, string? keySection = null)
    {
        // Redirect to the overload that utilizes AcceptLocales.
        return GetLocalizedAsync(key, AcceptLocales ?? throw new NoNullAllowedException(nameof(AcceptLocales)));
    }

    /// <inheritdoc />
    public async Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null)
    {
        string? localization;

        if (acceptLocales is not null)
        {
            // Try each locale in order until a valid localization is found.
            foreach (string locale in acceptLocales)
            {
                localization = await _translationService.TranslateAsync(key, locale);
                if (!string.IsNullOrWhiteSpace(localization))
                    return localization;
            }
        }

        localization = await _translationService.TranslateAsync(key, _defaultLocale);
        if (!string.IsNullOrWhiteSpace(localization))
            return localization;

        return key;
    }
}
