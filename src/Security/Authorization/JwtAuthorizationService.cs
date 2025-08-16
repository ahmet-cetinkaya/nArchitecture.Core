using System.Security.Claims;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authorization;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;
using NArchitecture.Core.Security.Authorization.Extensions;

namespace NArchitecture.Core.Security.Authorization;

public class JwtAuthorizationService<
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
>(
    IUserRepository<
        TUserId,
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserInGroupId,
        TUserOperationClaimId
    > userRepository
) : IAuthorizationService<TUserId, TOperationClaimId>
{
    public virtual Task<bool> HasPermissionAsync(
        TUserId userId,
        string permissionName,
        CancellationToken cancellationToken = default
    )
    {
        return userRepository.HasPermissionAsync(userId, permissionName, cancellationToken);
    }

    public virtual Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionName)
    {
        ICollection<string> userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasPermission = userOperationClaims.Contains(permissionName);
        return Task.FromResult(hasPermission);
    }

    public virtual Task<bool> HasAnyPermissionAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    )
    {
        return userRepository.HasAnyPermissionAsync(userId, permissionNames, cancellationToken);
    }

    public virtual Task<bool> HasAnyPermissionAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames)
    {
        ICollection<string> userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasAnyPermission = permissionNames.Any(userOperationClaims.Contains);
        return Task.FromResult(hasAnyPermission);
    }

    public virtual Task<bool> HasAllPermissionsAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    )
    {
        return userRepository.HasAllPermissionsAsync(userId, permissionNames, cancellationToken);
    }

    public virtual Task<bool> HasAllPermissionsAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames)
    {
        ICollection<string> userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasAllPermissions = permissionNames.All(userOperationClaims.Contains);
        return Task.FromResult(hasAllPermissions);
    }

    public virtual Task<ICollection<OperationClaim<TOperationClaimId>>> GetUserOperationClaimsAsync(
        TUserId userId,
        CancellationToken cancellationToken = default
    )
    {
        return userRepository.GetOperationClaimsAsync(userId, cancellationToken);
    }
}
