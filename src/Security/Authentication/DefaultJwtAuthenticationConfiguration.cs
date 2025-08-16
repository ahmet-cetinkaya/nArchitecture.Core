using NArchitecture.Core.Security.Abstractions.Authentication;

namespace NArchitecture.Core.Security.Authentication;

public class DefaultJwtAuthenticationConfiguration : IJwtAuthenticationConfiguration
{
    public string SecurityKey { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public TimeSpan AccessTokenExpiration { get; init; }
    public TimeSpan RefreshTokenTTL { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(5);
    public bool ValidateIssuerSigningKey { get; init; } = true;
    public bool ValidateAudience { get; init; } = true;
    public bool ValidateIssuer { get; init; } = true;
    public bool ValidateLifetime { get; init; } = true;
    public bool RequireExpirationTime { get; init; } = true;

    public DefaultJwtAuthenticationConfiguration(
        string securityKey,
        string issuer,
        string audience,
        TimeSpan accessTokenExpiration
    )
    {
        if (string.IsNullOrEmpty(securityKey))
            throw new ArgumentException("Security key cannot be null or empty.", nameof(securityKey));
        if (string.IsNullOrEmpty(issuer))
            throw new ArgumentException("Issuer cannot be null or empty.", nameof(issuer));
        if (string.IsNullOrEmpty(audience))
            throw new ArgumentException("Audience cannot be null or empty.", nameof(audience));
        if (accessTokenExpiration <= TimeSpan.Zero)
            throw new ArgumentException("Access token expiration must be greater than zero.", nameof(accessTokenExpiration));
        if (securityKey.Length < 32)
            throw new ArgumentException(
                "Security key must be at least 32 characters long for adequate security.",
                nameof(securityKey)
            );

        SecurityKey = securityKey;
        Issuer = issuer;
        Audience = audience;
        AccessTokenExpiration = accessTokenExpiration;
    }

    private const string DefaultUserNotFound = "The specified user could not be found.";
    private const string DefaultInvalidPassword =
        "The provided password is incorrect. Please check your credentials and try again.";
    private const string DefaultInvalidRefreshToken = "The refresh token is invalid or has been tampered with.";
    private const string DefaultTokenRevoked = "This token has been revoked and is no longer valid for use.";
    private const string DefaultTokenExpired = "The authentication token has expired. Please log in again to continue.";
    private const string DefaultTokenAlreadyRevoked = "This token has already been revoked. No further action is needed.";

    public virtual ValueTask<string> GetUserNotFoundMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultUserNotFound);
    }

    public virtual ValueTask<string> GetInvalidPasswordMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultInvalidPassword);
    }

    public virtual ValueTask<string> GetInvalidRefreshTokenMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultInvalidRefreshToken);
    }

    public virtual ValueTask<string> GetTokenRevokedMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultTokenRevoked);
    }

    public virtual ValueTask<string> GetTokenExpiredMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultTokenExpired);
    }

    public virtual ValueTask<string> GetTokenAlreadyRevokedMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultTokenAlreadyRevoked);
    }
}
