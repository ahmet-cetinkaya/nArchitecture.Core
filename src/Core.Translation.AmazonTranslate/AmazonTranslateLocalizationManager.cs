using Amazon.Translate;
using Amazon.Translate.Model;
using NArchitecture.Core.Translation.Abstractions;

namespace NArchitecture.Core.Translation.AmazonTranslate;

/// <summary>
/// Implements translation services using Amazon Translate.
/// </summary>
public class AmazonTranslateLocalizationManager : ITranslationService
{
    private readonly IAmazonTranslate _client;

    public AmazonTranslateLocalizationManager(AmazonTranslateConfiguration configuration)
        : this(new AmazonTranslateClient(configuration.AccessKey, configuration.SecretKey, configuration.RegionEndpoint)) { }

    public AmazonTranslateLocalizationManager(IAmazonTranslate client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<string> TranslateAsync(string text, string to, string from = "en")
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text to translate cannot be empty.", nameof(text));
        if (string.IsNullOrEmpty(to))
            throw new ArgumentException("Target language code cannot be empty.", nameof(to));
        if (string.IsNullOrEmpty(from))
            throw new ArgumentException("Source language code cannot be empty.", nameof(from));

        TranslateTextRequest request = new()
        {
            SourceLanguageCode = from,
            TargetLanguageCode = to,
            Text = text,
        };

        TranslateTextResponse response = await _client.TranslateTextAsync(request);
        return response.TranslatedText;
    }
}
