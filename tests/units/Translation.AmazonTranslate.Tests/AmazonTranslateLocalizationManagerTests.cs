using Amazon.Translate;
using Amazon.Translate.Model;
using Moq;
using Shouldly;

namespace NArchitecture.Core.Translation.AmazonTranslate.Tests;

[Trait("Category", "Unit")]
public class AmazonTranslateLocalizationManagerTests : IDisposable
{
    private readonly Mock<IAmazonTranslate> _mockTranslateClient;
    private readonly AmazonTranslateLocalizationManager _manager;

    public AmazonTranslateLocalizationManagerTests()
    {
        _mockTranslateClient = new Mock<IAmazonTranslate>();
        _manager = new AmazonTranslateLocalizationManager(_mockTranslateClient.Object);
    }

    [Theory(DisplayName = "TranslateAsync should successfully translate text")]
    [InlineData("Hello", "tr", "en", "Merhaba")]
    [InlineData("World", "es", "en", "Mundo")]
    public async Task TranslateAsync_ShouldTranslateText_Successfully(
        string sourceText,
        string targetLang,
        string sourceLang,
        string expectedTranslation
    )
    {
        // Arrange
        _ = _mockTranslateClient
            .Setup(x =>
                x.TranslateTextAsync(
                    It.Is<TranslateTextRequest>(r =>
                        r.Text == sourceText && r.TargetLanguageCode == targetLang && r.SourceLanguageCode == sourceLang
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TranslateTextResponse { TranslatedText = expectedTranslation });

        // Act
        string result = await _manager.TranslateAsync(sourceText, targetLang, sourceLang);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldBe(expectedTranslation);
        _mockTranslateClient.Verify(
            x =>
                x.TranslateTextAsync(
                    It.Is<TranslateTextRequest>(r =>
                        r.Text == sourceText && r.TargetLanguageCode == targetLang && r.SourceLanguageCode == sourceLang
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory(DisplayName = "TranslateAsync should throw exception for invalid language codes")]
    [InlineData("Hello", "invalid", "en")]
    [InlineData("World", "es", "invalid")]
    public async Task TranslateAsync_ShouldThrowException_WhenInvalidLanguageCodes(
        string sourceText,
        string targetLang,
        string sourceLang
    )
    {
        // Arrange
        _ = _mockTranslateClient
            .Setup(x => x.TranslateTextAsync(It.IsAny<TranslateTextRequest>(), default))
            .ThrowsAsync(new InvalidRequestException("Invalid language code"));

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidRequestException>(
            async () => await _manager.TranslateAsync(sourceText, targetLang, sourceLang)
        );
    }

    [Fact(DisplayName = "TranslateAsync should throw exception for empty text")]
    public async Task TranslateAsync_ShouldThrowException_WhenEmptyText()
    {
        // Arrange
        string emptyText = string.Empty;

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentException>(async () => await _manager.TranslateAsync(emptyText, "tr", "en"));
    }

    [Theory(DisplayName = "TranslateAsync should handle service errors gracefully")]
    [InlineData("Service Unavailable")]
    [InlineData("Internal Error")]
    public async Task TranslateAsync_ShouldHandleServiceErrors_Gracefully(string errorMessage)
    {
        // Arrange
        _ = _mockTranslateClient
            .Setup(x => x.TranslateTextAsync(It.IsAny<TranslateTextRequest>(), default))
            .ThrowsAsync(new AmazonTranslateException(errorMessage));

        // Act & Assert
        _ = await Should.ThrowAsync<AmazonTranslateException>(async () => await _manager.TranslateAsync("Hello", "tr", "en"));
    }

    [Theory(DisplayName = "TranslateAsync should throw ArgumentException for invalid inputs")]
    [InlineData("", "tr", "en", "Text to translate cannot be empty.")]
    [InlineData("Hello", "", "en", "Target language code cannot be empty.")]
    [InlineData("Hello", "tr", "", "Source language code cannot be empty.")]
    public async Task TranslateAsync_ShouldThrowArgumentException_WhenInvalidInput(
        string text,
        string targetLang,
        string sourceLang,
        string expectedErrorMessage
    )
    {
        // Act & Assert
        ArgumentException exception = await Should.ThrowAsync<ArgumentException>(
            async () => await _manager.TranslateAsync(text, targetLang, sourceLang)
        );
        exception.Message.ShouldContain(expectedErrorMessage);
        _mockTranslateClient.Verify(
            x => x.TranslateTextAsync(It.IsAny<TranslateTextRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    public void Dispose()
    {
        _mockTranslateClient.VerifyAll();
        _mockTranslateClient.Verify(x => x.Dispose(), Times.Never);
    }
}
