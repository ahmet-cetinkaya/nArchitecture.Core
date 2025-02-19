using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NArchitecture.Core.Security.Abstractions.Authentication;
using Shouldly;

namespace NArchitecture.Core.Security.WebApi.Tests;

[Trait("Category", "Unit")]
public class ServiceCollectionJwtAuthenticationExtensionsTests
{
    [Fact(DisplayName = "ConfigureJwtAuthentication should add JWT authentication with valid configuration")]
    public async Task ConfigureJwtAuthentication_ShouldAddJwtAuthentication_WithValidConfiguration()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        mockJwtConfig.Setup(c => c.Audience).Returns("test-audience");
        mockJwtConfig.Setup(c => c.Issuer).Returns("test-issuer");
        mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateIssuerSigningKey).Returns(true);
        mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.Zero);
        mockJwtConfig.Setup(c => c.RequireExpirationTime).Returns(true);
        mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(30));
        mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromDays(7));

        // Act
        services.ConfigureJwtAuthentication(mockJwtConfig.Object);
        var provider = services.BuildServiceProvider();
        var authenticationScheme = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Assert
        var scheme = await authenticationScheme.GetDefaultAuthenticateSchemeAsync();
        scheme.ShouldNotBeNull();
        scheme.Name.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should configure token validation parameters correctly")]
    public void ConfigureJwtAuthentication_ShouldConfigureTokenValidationParameters_Correctly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        const string securityKey = "very-long-secure-key-for-testing-purposes-min-16-chars";
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns(securityKey);
        mockJwtConfig.Setup(c => c.Audience).Returns("test-audience");
        mockJwtConfig.Setup(c => c.Issuer).Returns("test-issuer");
        mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        mockJwtConfig.Setup(c => c.ValidateIssuerSigningKey).Returns(true);
        mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.Zero);
        mockJwtConfig.Setup(c => c.RequireExpirationTime).Returns(true);
        mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(30));
        mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromDays(7));

        // Act
        services.ConfigureJwtAuthentication(mockJwtConfig.Object);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.ShouldSatisfyAllConditions(
            param => param.ValidateIssuerSigningKey.ShouldBeTrue(),
            param => param.IssuerSigningKey.ShouldBeOfType<SymmetricSecurityKey>(),
            param => param.ValidateAudience.ShouldBeTrue(),
            param => param.ValidAudience.ShouldBe("test-audience"),
            param => param.ValidateIssuer.ShouldBeTrue(),
            param => param.ValidIssuer.ShouldBe("test-issuer"),
            param => param.ValidateLifetime.ShouldBeTrue(),
            param => param.ClockSkew.ShouldBe(TimeSpan.Zero),
            param => param.RequireExpirationTime.ShouldBeTrue()
        );
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when security key is null or empty")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenSecurityKeyIsNullOrEmpty()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns(string.Empty);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when security key is invalid")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenSecurityKeyIsInvalid(string? securityKey)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns(securityKey!);

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
        exception.ParamName.ShouldBe("SecurityKey");
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when security key is too short")]
    [InlineData("short")]
    [InlineData("12345")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenSecurityKeyIsTooShort(string shortKey)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns(shortKey);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
        exception.Message.ShouldContain("16 characters");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when ValidateIssuer is true but Issuer is empty")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenValidateIssuerIsTrueButIssuerIsEmpty()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        mockJwtConfig.Setup(c => c.Issuer).Returns(string.Empty);

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
        exception.ParamName.ShouldBe("Issuer");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when ValidateAudience is true but Audience is empty")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenValidateAudienceIsTrueButAudienceIsEmpty()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        mockJwtConfig.Setup(c => c.Audience).Returns(string.Empty);

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
        exception.ParamName.ShouldBe("Audience");
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when token lifetimes are invalid")]
    [InlineData(-1, 10)]
    [InlineData(0, 10)]
    [InlineData(10, -1)]
    [InlineData(10, 0)]
    [InlineData(10, 5)]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenTokenLifetimesAreInvalid(
        int accessTokenMinutes,
        int refreshTokenMinutes
    )
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(accessTokenMinutes));
        mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromMinutes(refreshTokenMinutes));

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when clock skew is negative")]
    [InlineData(-1)]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenClockSkewIsNegative(int skewMinutes)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.FromMinutes(skewMinutes));

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
        exception.Message.ShouldContain("negative");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should return services collection for chaining")]
    public void ConfigureJwtAuthentication_ShouldReturnServicesCollection_ForChaining()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");

        // Act
        var result = services.ConfigureJwtAuthentication(mockJwtConfig.Object);

        // Assert
        result.ShouldBe(services);
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when configuration is null")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenConfigurationIsNull()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(null!));
    }
}
