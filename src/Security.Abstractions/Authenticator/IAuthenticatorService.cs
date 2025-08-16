using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.Abstractions.Authenticator;

public interface IAuthenticatorService<
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>
{
    Task<
        UserAuthenticator<
            TUserAuthenticatorId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserGroupId,
            TUserId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    > CreateAsync(TUserId userId, AuthenticatorType type, string? destination, CancellationToken cancellationToken = default);
    Task VerifyAsync(TUserId userId, string code, CancellationToken cancellationToken = default);
    Task AttemptAsync(TUserId userId, string? destination, CancellationToken cancellationToken = default);
    Task DeleteAsync(TUserId userId, CancellationToken cancellationToken = default);
}
