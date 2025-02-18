using System.Security.Claims;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authorization;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;
using NArchitecture.Core.Security.Authorization.Extensions;

namespace NArchitecture.Core.Security.Authorization;

public class JwtAuthorizationService<TUserId, TUserAuthenticatorId, TOperationClaimId>(
    IUserRepository<TUserId, TUserAuthenticatorId, TOperationClaimId> userRepository
) : IAuthorizationService<TUserId, TOperationClaimId>
{
    private readonly IUserRepository<TUserId, TUserAuthenticatorId, TOperationClaimId> _userRepository = userRepository;

    public virtual Task<bool> HasPermissionAsync(
        TUserId userId,
        string permissionName,
        CancellationToken cancellationToken = default
    ) => _userRepository.HasPermissionAsync(userId, permissionName, cancellationToken);

    public virtual Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionName)
    {
        var userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasPermission = userOperationClaims.Contains(permissionName);
        return Task.FromResult(hasPermission);
    }

    public virtual Task<bool> HasAnyPermissionAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    ) => _userRepository.HasAnyPermissionAsync(userId, permissionNames, cancellationToken);

    public virtual Task<bool> HasAnyPermissionAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames)
    {
        var userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasAnyPermission = permissionNames.Any(userOperationClaims.Contains);
        return Task.FromResult(hasAnyPermission);
    }

    public virtual Task<bool> HasAllPermissionsAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    ) => _userRepository.HasAllPermissionsAsync(userId, permissionNames, cancellationToken);

    public virtual Task<bool> HasAllPermissionsAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames)
    {
        var userOperationClaims = principal.GetOperationClaims() ?? [];
        bool hasAllPermissions = permissionNames.All(userOperationClaims.Contains);
        return Task.FromResult(hasAllPermissions);
    }

    public virtual Task<ICollection<OperationClaim<TOperationClaimId>>> GetUserOperationClaimsAsync(
        TUserId userId,
        CancellationToken cancellationToken = default
    ) => _userRepository.GetOperationClaimsAsync(userId, cancellationToken);
}
