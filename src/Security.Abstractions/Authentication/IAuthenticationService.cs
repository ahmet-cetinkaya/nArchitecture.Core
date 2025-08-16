using NArchitecture.Core.Security.Abstractions.Authentication.Models;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

/// <summary>
/// Defines the core authentication operations.
/// </summary>
/// <typeparam name="TOperationClaimId">The type of the operation claim identifier.</typeparam>
/// <typeparam name="TRefreshTokenId">The type of the refresh token identifier.</typeparam>
/// <typeparam name="TUserAuthenticatorId">The type of the user authenticator identifier.</typeparam>
/// <typeparam name="TUserGroupId">The type of the user group identifier.</typeparam>
/// <typeparam name="TUserId">The type of the user identifier.</typeparam>
/// <typeparam name="TUserInGroupId">The type of the user in group identifier.</typeparam>
/// <typeparam name="TUserOperationClaimId">The type of the user operation claim identifier.</typeparam>
public interface IAuthenticationService<
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>
{
    Task<AuthenticationResponse> LoginAsync(
        LoginRequest<
            TOperationClaimId,
            TRefreshTokenId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > request,
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
