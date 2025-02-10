using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Localization.WebApi;
using Shouldly;

namespace Core.Localization.WebApi.Tests;

public class LocalizationMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly DefaultHttpContext _httpContext;
    private readonly LocalizationMiddleware _middleware;

    public LocalizationMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _httpContext = new DefaultHttpContext();

        // Setup the next delegate with explicit Task return
        var task = Task.CompletedTask;
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(task);

        _middleware = new LocalizationMiddleware(_nextMock.Object);
    }

    [Theory(DisplayName = "Should set AcceptLocales from Accept-Language header")]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", new[] { "tr-TR", "tr", "en" })]
    [InlineData("en-US,en;q=0.9", new[] { "en-US", "en" })]
    [InlineData("fr-FR", new[] { "fr-FR" })]
    public async Task Invoke_WithValidAcceptLanguageHeader_ShouldSetAcceptLocales(
        string acceptLanguageHeader,
        string[] expectedLocales
    )
    {
        // Arrange
        _httpContext.Request.Headers[HeaderNames.AcceptLanguage] = new StringValues(acceptLanguageHeader);

        // Act
        await _middleware.Invoke(_httpContext, _localizationServiceMock.Object);

        // Assert
        var immutableExpectedLocales = expectedLocales.ToImmutableArray();
        _localizationServiceMock.VerifySet(x =>
            x.AcceptLocales = It.Is<ImmutableArray<string>>(locales =>
                locales.Length == immutableExpectedLocales.Length && locales.All(l => immutableExpectedLocales.Contains(l))
            )
        );
        verifyNextMiddlewareWasCalled();
    }

    [Fact(DisplayName = "Should handle empty Accept-Language header")]
    public async Task Invoke_WithEmptyAcceptLanguageHeader_ShouldNotSetAcceptLocales()
    {
        // Arrange
        _httpContext.Request.Headers[HeaderNames.AcceptLanguage] = StringValues.Empty;

        // Act
        await _middleware.Invoke(_httpContext, _localizationServiceMock.Object);

        // Assert
        _localizationServiceMock.VerifyNoOtherCalls();
        verifyNextMiddlewareWasCalled();
    }

    [Theory(DisplayName = "Should handle quality values in Accept-Language header")]
    [InlineData("tr;q=1.0,en;q=0.8,fr;q=0.5", new[] { "tr", "en", "fr" })]
    [InlineData("en;q=0.5,tr;q=1.0,fr;q=0.8", new[] { "tr", "fr", "en" })]
    public async Task Invoke_WithQualityValues_ShouldOrderByQuality(string acceptLanguageHeader, string[] expectedOrderedLocales)
    {
        // Arrange
        _httpContext.Request.Headers[HeaderNames.AcceptLanguage] = new StringValues(acceptLanguageHeader);

        // Act
        await _middleware.Invoke(_httpContext, _localizationServiceMock.Object);

        // Assert
        var immutableExpectedLocales = expectedOrderedLocales.ToImmutableArray();
        _localizationServiceMock.VerifySet(x =>
            x.AcceptLocales = It.Is<ImmutableArray<string>>(locales =>
                locales.Length == immutableExpectedLocales.Length
                && Enumerable.Range(0, locales.Length).All(i => locales[i] == expectedOrderedLocales[i])
            )
        );
    }

    [Fact(DisplayName = "Should handle malformed Accept-Language header")]
    public async Task Invoke_WithMalformedAcceptLanguageHeader_ShouldNotThrowException()
    {
        // Arrange
        _httpContext.Request.Headers[HeaderNames.AcceptLanguage] = "invalid,header;q=invalid";

        // Act
        Func<Task> act = () => _middleware.Invoke(_httpContext, _localizationServiceMock.Object);

        // Assert
        var exception = await Record.ExceptionAsync(act);
        exception.ShouldBe(null);
        verifyNextMiddlewareWasCalled();
    }

    [Fact(DisplayName = "Should throw when next delegate is null")]
    public void Constructor_WithNullNextDelegate_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new LocalizationMiddleware(null!));
    }

    [Theory(DisplayName = "Should handle various Accept-Language header formats")]
    [InlineData("*", new[] { "*" })]
    [InlineData("en-US;q=0.8,*;q=0.1", new[] { "en-US", "*" })]
    [InlineData("zh-CN,zh;q=0.9,en;q=0.8", new[] { "zh-CN", "zh", "en" })]
    public async Task Invoke_WithVariousHeaderFormats_ShouldHandleCorrectly(string acceptLanguageHeader, string[] expectedLocales)
    {
        // Arrange
        _httpContext.Request.Headers[HeaderNames.AcceptLanguage] = new StringValues(acceptLanguageHeader);

        // Act
        await _middleware.Invoke(_httpContext, _localizationServiceMock.Object);

        // Assert
        var immutableExpectedLocales = expectedLocales.ToImmutableArray();
        _localizationServiceMock.VerifySet(x =>
            x.AcceptLocales = It.Is<ImmutableArray<string>>(locales =>
                locales.SequenceEqual(immutableExpectedLocales, EqualityComparer<string>.Default)
            )
        );
    }

    private void verifyNextMiddlewareWasCalled()
    {
        var expectedContext = _httpContext;
        _nextMock.Verify(x => x.Invoke(It.Is<HttpContext>(ctx => ReferenceEquals(ctx, expectedContext))), Times.Once);
    }
}
