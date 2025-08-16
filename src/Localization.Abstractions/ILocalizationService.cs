namespace NArchitecture.Core.Localization.Abstractions;

public interface ILocalizationService
{
    /// <summary>
    /// Gets or sets the collection of accepted locales used to determine the localization order.
    /// </summary>
    ICollection<string>? AcceptLocales { get; set; }

    /// <summary>
    /// Gets the localized string for the given key using the accepted locales.
    /// </summary>
    /// <param name="key">The key identifying the localized string.</param>
    /// <param name="keySection">Optional section for grouping localization keys.</param>
    /// <returns>The localized string.</returns>
    Task<string> GetLocalizedAsync(string key, string? keySection = null);

    /// <summary>
    /// Gets the localized string for the given key using the provided collection of accepted locales.
    /// </summary>
    /// <param name="key">The key identifying the localized string.</param>
    /// <param name="acceptLocales">The collection of locales to attempt, in priority order.</param>
    /// <param name="keySection">Optional section for grouping localization keys.</param>
    /// <returns>The localized string.</returns>
    Task<string> GetLocalizedAsync(string key, ICollection<string> acceptLocales, string? keySection = null);
}
