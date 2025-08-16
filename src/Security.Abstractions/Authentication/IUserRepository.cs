using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

public interface IUserRepository<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserInGroupId,
    TUserOperationClaimId
>
{
    Task<bool> HasPermissionAsync(TId userId, string permissionName, CancellationToken cancellationToken = default);

    Task<bool> HasAnyPermissionAsync(TId id, IEnumerable<string> permissionNames, CancellationToken cancellationToken = default);

    Task<bool> HasAllPermissionsAsync(TId id, IEnumerable<string> permissionNames, CancellationToken cancellationToken = default);

    Task<ICollection<OperationClaim<TOperationClaimId>>> GetOperationClaimsAsync(
        TId id,
        CancellationToken cancellationToken = default
    );

    Task<User<
        TId,
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserInGroupId,
        TUserOperationClaimId
    >?> GetByIdAsync(TId? userId, CancellationToken cancellationToken);
}
