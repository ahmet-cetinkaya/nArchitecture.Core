using NArchitecture.Core.Security.Authentication;
using Shouldly;

namespace Core.Security.Tests.Authentication;

[Trait("Category", "Unit")]
public class DefaultJwtAuthenticationConfigurationTests
{
    private const string ValidSecurityKey = "this-is-a-valid-security-key-with-minimum-length";
    private const string ValidIssuer = "valid-issuer";
    private const string ValidAudience = "valid-audience";
    private static readonly TimeSpan ValidExpiration = TimeSpan.FromHours(1);

    [Fact(DisplayName = "Should create valid configuration with default values")]
    public void Should_Create_Valid_Configuration_With_Default_Values()
    {
        // Arrange & Act
        var config = new DefaultJwtAuthenticationConfiguration(ValidSecurityKey, ValidIssuer, ValidAudience, ValidExpiration);

        // Assert
        config.SecurityKey.ShouldBe(ValidSecurityKey);
        config.Issuer.ShouldBe(ValidIssuer);
        config.Audience.ShouldBe(ValidAudience);
        config.AccessTokenExpiration.ShouldBe(ValidExpiration);
        config.RefreshTokenTTL.ShouldBe(TimeSpan.FromDays(7));
        config.ClockSkew.ShouldBe(TimeSpan.FromMinutes(5));
        config.ValidateIssuerSigningKey.ShouldBeTrue();
        config.ValidateAudience.ShouldBeTrue();
        config.ValidateIssuer.ShouldBeTrue();
        config.ValidateLifetime.ShouldBeTrue();
        config.RequireExpirationTime.ShouldBeTrue();
    }

    [Theory(DisplayName = "Should throw ArgumentException for invalid constructor parameters")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null, ValidIssuer, ValidAudience)]
    [InlineData("", ValidIssuer, ValidAudience)]
    [InlineData("short", ValidIssuer, ValidAudience)]
    [InlineData(ValidSecurityKey, null, ValidAudience)]
    [InlineData(ValidSecurityKey, "", ValidAudience)]
    [InlineData(ValidSecurityKey, ValidIssuer, null)]
    [InlineData(ValidSecurityKey, ValidIssuer, "")]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    public void Should_Throw_ArgumentException_For_Invalid_Parameters(string securityKey, string issuer, string audience)
    {
        // Arrange & Act & Assert
        _ = Should.Throw<ArgumentException>(
            () => new DefaultJwtAuthenticationConfiguration(securityKey, issuer, audience, ValidExpiration)
        );
    }

    [Fact(DisplayName = "Should throw ArgumentException for invalid expiration")]
    public void Should_Throw_ArgumentException_For_Invalid_Expiration()
    {
        // Arrange & Act & Assert
        _ = Should.Throw<ArgumentException>(
            () => new DefaultJwtAuthenticationConfiguration(ValidSecurityKey, ValidIssuer, ValidAudience, TimeSpan.Zero)
        );
    }

    [Fact(DisplayName = "Should return correct message for GetUserNotFoundMessageAsync")]
    public async Task Should_Return_Correct_UserNotFound_Message()
    {
        // Arrange
        var config = new DefaultJwtAuthenticationConfiguration(ValidSecurityKey, ValidIssuer, ValidAudience, ValidExpiration);

        // Act
        string message = await config.GetUserNotFoundMessageAsync();

        // Assert
        message.ShouldBe("The specified user could not be found.");
    }

    [Fact(DisplayName = "Should return correct message for all error scenarios")]
    public async Task Should_Return_Correct_Error_Messages()
    {
        // Arrange
        var config = new DefaultJwtAuthenticationConfiguration(ValidSecurityKey, ValidIssuer, ValidAudience, ValidExpiration);

        // Act & Assert
        (await config.GetInvalidPasswordMessageAsync()).ShouldBe(
            "The provided password is incorrect. Please check your credentials and try again."
        );
        (await config.GetInvalidRefreshTokenMessageAsync()).ShouldBe("The refresh token is invalid or has been tampered with.");
        (await config.GetTokenRevokedMessageAsync()).ShouldBe("This token has been revoked and is no longer valid for use.");
        (await config.GetTokenExpiredMessageAsync()).ShouldBe(
            "The authentication token has expired. Please log in again to continue."
        );
        (await config.GetTokenAlreadyRevokedMessageAsync()).ShouldBe(
            "This token has already been revoked. No further action is needed."
        );
    }
}
