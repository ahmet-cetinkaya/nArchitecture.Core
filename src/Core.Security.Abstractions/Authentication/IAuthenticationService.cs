using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authentication.Models;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

/// <summary>
/// Defines the core authentication operations.
/// </summary>
/// <typeparam name="TUserId">The type of the user identifier.</typeparam>
/// <typeparam name="TUserAuthenticatorId">The type of the user authenticator identifier.</typeparam>
public interface IAuthenticationService<TUserId, TUserAuthenticatorId>
{
    Task<AuthenticationResponse> LoginAsync(
        LoginRequest<TUserId, TUserAuthenticatorId> request,
        CancellationToken cancellationToken = default
    );
    Task<RefreshTokenResponse> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default
    );
    Task RevokeRefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default
    );
    Task RevokeAllRefreshTokensAsync(TUserId userId, string? reason = null, CancellationToken cancellationToken = default);
}
