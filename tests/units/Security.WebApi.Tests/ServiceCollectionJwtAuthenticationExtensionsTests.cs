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
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        _ = mockJwtConfig.Setup(c => c.Audience).Returns("test-audience");
        _ = mockJwtConfig.Setup(c => c.Issuer).Returns("test-issuer");
        _ = mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateIssuerSigningKey).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.Zero);
        _ = mockJwtConfig.Setup(c => c.RequireExpirationTime).Returns(true);
        _ = mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(30));
        _ = mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromDays(7));

        // Act
        _ = services.ConfigureJwtAuthentication(mockJwtConfig.Object);
        ServiceProvider provider = services.BuildServiceProvider();
        IAuthenticationSchemeProvider authenticationScheme = provider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Assert
        AuthenticationScheme? scheme = await authenticationScheme.GetDefaultAuthenticateSchemeAsync();
        _ = scheme.ShouldNotBeNull();
        scheme.Name.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should configure token validation parameters correctly")]
    public void ConfigureJwtAuthentication_ShouldConfigureTokenValidationParameters_Correctly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        const string securityKey = "very-long-secure-key-for-testing-purposes-min-16-chars";
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns(securityKey);
        _ = mockJwtConfig.Setup(c => c.Audience).Returns("test-audience");
        _ = mockJwtConfig.Setup(c => c.Issuer).Returns("test-issuer");
        _ = mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ValidateIssuerSigningKey).Returns(true);
        _ = mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.Zero);
        _ = mockJwtConfig.Setup(c => c.RequireExpirationTime).Returns(true);
        _ = mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(30));
        _ = mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromDays(7));

        // Act
        _ = services.ConfigureJwtAuthentication(mockJwtConfig.Object);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<JwtBearerOptions> options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        JwtBearerOptions jwtOptions = options.Get(JwtBearerDefaults.AuthenticationScheme);

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
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns(string.Empty);

        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
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
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns(securityKey!);

        // Act & Assert
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            services.ConfigureJwtAuthentication(mockJwtConfig.Object)
        );
        exception.ParamName.ShouldBe("jwtConfiguration.SecurityKey");
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when security key is too short")]
    [InlineData("short")]
    [InlineData("12345")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenSecurityKeyIsTooShort(string shortKey)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns(shortKey);

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            services.ConfigureJwtAuthentication(mockJwtConfig.Object)
        );
        exception.Message.ShouldContain("16 characters");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when ValidateIssuer is true but Issuer is empty")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenValidateIssuerIsTrueButIssuerIsEmpty()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        _ = mockJwtConfig.Setup(c => c.ValidateIssuer).Returns(true);
        _ = mockJwtConfig.Setup(c => c.Issuer).Returns(string.Empty);

        // Act & Assert
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            services.ConfigureJwtAuthentication(mockJwtConfig.Object)
        );
        exception.ParamName.ShouldBe("jwtConfiguration.Issuer");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when ValidateAudience is true but Audience is empty")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenValidateAudienceIsTrueButAudienceIsEmpty()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        _ = mockJwtConfig.Setup(c => c.ValidateAudience).Returns(true);
        _ = mockJwtConfig.Setup(c => c.Audience).Returns(string.Empty);

        // Act & Assert
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            services.ConfigureJwtAuthentication(mockJwtConfig.Object)
        );
        exception.ParamName.ShouldBe("jwtConfiguration.Audience");
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
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        _ = mockJwtConfig.Setup(c => c.ValidateLifetime).Returns(true);
        _ = mockJwtConfig.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(accessTokenMinutes));
        _ = mockJwtConfig.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromMinutes(refreshTokenMinutes));

        // Act & Assert
        _ = Should.Throw<ArgumentException>(() => services.ConfigureJwtAuthentication(mockJwtConfig.Object));
    }

    [Theory(DisplayName = "ConfigureJwtAuthentication should throw when clock skew is negative")]
    [InlineData(-1)]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenClockSkewIsNegative(int skewMinutes)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");
        _ = mockJwtConfig.Setup(c => c.ClockSkew).Returns(TimeSpan.FromMinutes(skewMinutes));

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            services.ConfigureJwtAuthentication(mockJwtConfig.Object)
        );
        exception.ParamName.ShouldBe("jwtConfiguration.ClockSkew");
        exception.Message.ShouldContain("negative");
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should return services collection for chaining")]
    public void ConfigureJwtAuthentication_ShouldReturnServicesCollection_ForChaining()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var mockJwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        _ = mockJwtConfig.Setup(c => c.SecurityKey).Returns("very-long-secure-key-for-testing-purposes-min-16-chars");

        // Act
        IServiceCollection result = services.ConfigureJwtAuthentication(mockJwtConfig.Object);

        // Assert
        result.ShouldBe(services);
    }

    [Fact(DisplayName = "ConfigureJwtAuthentication should throw when configuration is null")]
    public void ConfigureJwtAuthentication_ShouldThrow_WhenConfigurationIsNull()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => services.ConfigureJwtAuthentication(null!));
    }
}
