using Amazon.Translate;
using Amazon.Translate.Model;
using NArchitecture.Core.Translation.Abstraction;

namespace NArchitecture.Core.Translation.AmazonTranslate;

/// <summary>
/// Implements translation services using Amazon Translate.
/// </summary>
public class AmazonTranslateLocalizationManager(AmazonTranslateConfiguration configuration) : ITranslationService
{
    private readonly AmazonTranslateClient _client = new(
        configuration.AccessKey,
        configuration.SecretKey,
        configuration.RegionEndpoint
    );

    /// <inheritdoc/>
    public async Task<string> TranslateAsync(string text, string to, string from = "en")
    {
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
