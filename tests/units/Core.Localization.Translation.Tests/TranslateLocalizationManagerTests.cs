using System.Data;
using Moq;
using NArchitecture.Core.Translation.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Localization.Translation.Tests;

public class TranslateLocalizationManagerTests
{
    private readonly Mock<ITranslationService> _mockTranslationService;

    public TranslateLocalizationManagerTests()
    {
        _mockTranslationService = new Mock<ITranslationService>();
    }

    [Fact(DisplayName = "Should successfully translate text when translation exists")]
    public async Task GetLocalizedAsync_WhenTranslationExists_ShouldReturnTranslatedText()
    {
        // Arrange
        const string key = "hello";
        const string expectedTranslation = "Merhaba";
        _mockTranslationService.Setup(s => s.TranslateAsync(key, "tr", It.IsAny<string>())).ReturnsAsync(expectedTranslation);

        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = new[] { "tr" } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(expectedTranslation);
        _mockTranslationService.Verify(s => s.TranslateAsync(key, "tr", It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "Should fallback to default locale when translation not found")]
    public async Task GetLocalizedAsync_WhenTranslationNotFound_ShouldFallbackToDefaultLocale()
    {
        // Arrange
        const string key = "hello";
        const string defaultTranslation = "Hello";
        _mockTranslationService.Setup(s => s.TranslateAsync(key, "tr", It.IsAny<string>())).ReturnsAsync(string.Empty);
        _mockTranslationService.Setup(s => s.TranslateAsync(key, "en", It.IsAny<string>())).ReturnsAsync(defaultTranslation);

        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = new[] { "tr" } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(defaultTranslation);
        _mockTranslationService.Verify(s => s.TranslateAsync(key, "tr", It.IsAny<string>()), Times.Once);
        _mockTranslationService.Verify(s => s.TranslateAsync(key, "en", It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "Should return key itself when no translation found including default locale")]
    public async Task GetLocalizedAsync_WhenNoTranslationFoundIncludingDefault_ShouldReturnKey()
    {
        // Arrange
        const string key = "nonexistent_key";
        _mockTranslationService
            .Setup(s => s.TranslateAsync(key, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = new[] { "tr" } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(key);
    }

    [Fact(DisplayName = "Should throw exception when AcceptLocales is null")]
    public void GetLocalizedAsync_WhenAcceptLocalesIsNull_ShouldThrowException()
    {
        // Arrange
        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = null };

        // Act & Assert
        Should.Throw<NoNullAllowedException>(() => manager.GetLocalizedAsync("any_key").GetAwaiter().GetResult());
    }

    [Theory(DisplayName = "Should try multiple locales in order until finding translation")]
    [InlineData(new[] { "fr", "de", "tr" }, "tr")]
    [InlineData(new[] { "es", "it", "en" }, "en")]
    public async Task GetLocalizedAsync_WithMultipleLocales_ShouldTryInOrder(string[] locales, string expectedLocale)
    {
        // Arrange
        const string key = "hello";
        const string translation = "Translation";

        foreach (string locale in locales.Where(l => l != expectedLocale))
        {
            _mockTranslationService.Setup(s => s.TranslateAsync(key, locale, It.IsAny<string>())).ReturnsAsync(string.Empty);
        }

        _mockTranslationService.Setup(s => s.TranslateAsync(key, expectedLocale, It.IsAny<string>())).ReturnsAsync(translation);

        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = locales };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(translation);
        foreach (string locale in locales.TakeWhile(l => l != expectedLocale))
        {
            _mockTranslationService.Verify(s => s.TranslateAsync(key, locale, It.IsAny<string>()), Times.Once);
        }
    }

    [Fact(DisplayName = "Should handle empty translation service response")]
    public async Task GetLocalizedAsync_WhenTranslationServiceReturnsEmpty_ShouldHandleGracefully()
    {
        // Arrange
        const string key = "test_key";
        _mockTranslationService
            .Setup(s => s.TranslateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = new[] { "tr", "en" } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(key);
        _mockTranslationService.Verify(s => s.TranslateAsync(key, It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
    }

    [Theory(DisplayName = "Should handle various edge cases for translation keys")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetLocalizedAsync_WithEdgeCaseKeys_ShouldHandleGracefully(string key)
    {
        // Arrange
        var manager = new TranslateLocalizationManager(_mockTranslationService.Object) { AcceptLocales = new[] { "en" } };

        // Act
        string result = await manager.GetLocalizedAsync(key);

        // Assert
        result.ShouldBe(key);
        _mockTranslationService.Verify(s => s.TranslateAsync(key, It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
    }
}
