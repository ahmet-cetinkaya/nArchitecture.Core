using System.Security.Claims;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

/// <summary>
/// Defines the contract for authorization operations.
/// </summary>
public interface IAuthorizationService<TUserId, TOperationClaimId>
{
    Task<bool> HasPermissionAsync(TUserId userId, string permissionName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string permissionName);

    Task<bool> HasAnyPermissionAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    );
    Task<bool> HasAnyPermissionAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames);

    Task<bool> HasAllPermissionsAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    );
    Task<bool> HasAllPermissionsAsync(ClaimsPrincipal principal, IEnumerable<string> permissionNames);

    Task<ICollection<OperationClaim<TOperationClaimId>>> GetUserOperationClaimsAsync(
        TUserId userId,
        CancellationToken cancellationToken = default
    );
}
