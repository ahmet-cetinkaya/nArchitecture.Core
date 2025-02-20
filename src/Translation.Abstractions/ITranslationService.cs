namespace NArchitecture.Core.Translation.Abstractions;

/// <summary>
/// Defines methods for translating text between different languages.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates the specified text from one language to another.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="to">The target language code.</param>
    /// <param name="from">The source language code. Defaults to "en" (English).</param>
    /// <returns>The translated text.</returns>
    Task<string> TranslateAsync(string text, string to, string from = "en");
}
