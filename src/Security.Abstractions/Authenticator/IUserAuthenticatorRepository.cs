using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authenticator;

public interface IUserAuthenticatorRepository<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>
{
    Task<
        UserAuthenticator<TId, TOperationClaimId, TRefreshTokenId, TUserGroupId, TUserId, TUserInGroupId, TUserOperationClaimId>
    > AddAsync(
        UserAuthenticator<
            TId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > authenticator,
        CancellationToken cancellationToken
    );

    Task<
        UserAuthenticator<TId, TOperationClaimId, TRefreshTokenId, TUserGroupId, TUserId, TUserInGroupId, TUserOperationClaimId>
    > DeleteAsync(
        UserAuthenticator<
            TId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > authenticator,
        CancellationToken cancellationToken
    );

    Task<UserAuthenticator<
        TId,
        TOperationClaimId,
        TRefreshTokenId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >?> GetByIdAsync(TUserId? userId, CancellationToken cancellationToken);

    Task<
        UserAuthenticator<TId, TOperationClaimId, TRefreshTokenId, TUserGroupId, TUserId, TUserInGroupId, TUserOperationClaimId>
    > UpdateAsync(
        UserAuthenticator<
            TId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > authenticator,
        CancellationToken cancellationToken
    );
}
