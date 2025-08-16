using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserGroupOperationClaim<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserGroupOperationClaimId,
    TUserId
> : BaseEntity<TId>
{
    public TUserGroupId UserGroupId { get; set; }
    public TOperationClaimId OperationClaimId { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public UserGroupOperationClaim()
    {
        UserGroupId = default!;
        OperationClaimId = default!;
    }

    public UserGroupOperationClaim(TUserGroupId userGroupId, TOperationClaimId operationClaimId)
    {
        UserGroupId = userGroupId;
        OperationClaimId = operationClaimId;
    }
}
