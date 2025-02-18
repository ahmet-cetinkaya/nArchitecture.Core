using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

public interface IUserGroupOperationClaimRepository<
    TId,
    TGroupId,
    TUserId,
    TUserAuthenticatorId,
    TUserInGroupId,
    TOperationClaimId
>
    : IAsyncRepository<
        UserGroupOperationClaim<TId, TGroupId, TUserId, TUserAuthenticatorId, TUserInGroupId, TOperationClaimId>,
        TId
    > { }
