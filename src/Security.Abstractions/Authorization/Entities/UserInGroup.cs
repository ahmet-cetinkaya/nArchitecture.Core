using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserInGroup<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
> : BaseEntity<TId>
{
    public required TUserId UserId { get; set; }
    public required TUserGroupId UserGroupId { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public UserInGroup()
    {
        UserId = default!;
        UserGroupId = default!;
    }

    public UserInGroup(TUserId userId, TUserGroupId userGroupId)
    {
        UserId = userId;
        UserGroupId = userGroupId;
    }


}
