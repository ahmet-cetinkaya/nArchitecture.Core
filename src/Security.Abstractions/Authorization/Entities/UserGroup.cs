using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserGroup<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserGroupOperationClaimId,
    TUserId
> : BaseEntity<TId>
{
    public string Name { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public UserGroup()
    {
        Name = default!;
    }

    public UserGroup(string name)
    {
        Name = name;
    }

    public virtual ICollection<
        UserGroupOperationClaim<
            TUserGroupOperationClaimId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserAuthenticatorId,
            TUserGroupId,
            TUserGroupOperationClaimId,
            TUserId
        >
    >? UserGroupOperationClaims { get; set; }
    public virtual ICollection<
        UserInGroup<
            TId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserAuthenticatorId,
            TId,
            TUserId,
            TUserGroupId,
            TUserGroupOperationClaimId
        >
    >? UserInGroups { get; set; }
}
