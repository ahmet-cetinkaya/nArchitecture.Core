namespace NArchitecture.Core.Security.Abstractions.Authentication;

public interface IJwtAuthenticationConfiguration
{
    string SecurityKey { get; }
    string Issuer { get; }
    string Audience { get; }
    TimeSpan AccessTokenExpiration { get; }
    TimeSpan RefreshTokenTTL { get; }
    TimeSpan ClockSkew { get; }
    bool ValidateIssuerSigningKey { get; }
    bool ValidateAudience { get; }
    bool ValidateIssuer { get; }
    bool ValidateLifetime { get; }
    bool RequireExpirationTime { get; }

    ValueTask<string> GetUserNotFoundMessageAsync(CancellationToken cancellationToken = default);
    ValueTask<string> GetInvalidPasswordMessageAsync(CancellationToken cancellationToken = default);
    ValueTask<string> GetInvalidRefreshTokenMessageAsync(CancellationToken cancellationToken = default);
    ValueTask<string> GetTokenRevokedMessageAsync(CancellationToken cancellationToken = default);
    ValueTask<string> GetTokenExpiredMessageAsync(CancellationToken cancellationToken = default);
    ValueTask<string> GetTokenAlreadyRevokedMessageAsync(CancellationToken cancellationToken = default);
}
