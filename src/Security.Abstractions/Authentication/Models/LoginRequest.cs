using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication.Models;

public readonly record struct LoginRequest<
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>(
    User<
        TUserId,
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserInGroupId,
        TUserOperationClaimId
    > User,
    string Password,
    string IpAddress
);
