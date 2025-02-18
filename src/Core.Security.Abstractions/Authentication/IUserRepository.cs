using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

public interface IUserRepository<TUserId, TUserAuthenticatorId, TOperationClaimId>
    : IAsyncRepository<User<TUserId, TUserAuthenticatorId>, TUserId>
{
    Task<bool> HasPermissionAsync(TUserId userId, string permissionName, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    );
    Task<bool> HasAllPermissionsAsync(
        TUserId userId,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default
    );
    Task<ICollection<OperationClaim<TOperationClaimId>>> GetOperationClaimsAsync(
        TUserId userId,
        CancellationToken cancellationToken = default
    );
}
