using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

public interface IRefreshTokenRepository<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>
{
    Task<RefreshToken<
        TId,
        TOperationClaimId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<
        ICollection<
            RefreshToken<
                TId,
                TOperationClaimId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >
        >
    > GetAllActiveByUserIdAsync(TUserId userId, CancellationToken cancellationToken = default);

    Task<
        RefreshToken<
            TRefreshTokenId,
            TOperationClaimId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    > AddAsync(
        RefreshToken<
            TRefreshTokenId,
            TOperationClaimId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > refreshTokenEntity,
        CancellationToken cancellationToken
    );

    Task<
        RefreshToken<
            TRefreshTokenId,
            TOperationClaimId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    > UpdateAsync(
        RefreshToken<
            TRefreshTokenId,
            TOperationClaimId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        > token,
        CancellationToken cancellationToken
    );
}
