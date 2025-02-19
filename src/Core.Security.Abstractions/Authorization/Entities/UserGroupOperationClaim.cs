using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserGroupOperationClaim<TId, TUserGroupId, TUserId, TUserAuthenticatorId, TUserInGroupId, TOperationClaimId>
    : BaseEntity<TId>
{
    public TUserGroupId UserGroupId { get; set; }
    public TOperationClaimId OperationClaimId { get; set; }

    public virtual UserGroup<TUserGroupId, TUserId, TUserAuthenticatorId, TUserInGroupId>? UserGroup { get; set; }
    public virtual OperationClaim<TOperationClaimId>? OperationClaim { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]

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
