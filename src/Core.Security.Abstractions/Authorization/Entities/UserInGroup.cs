using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserInGroup<TId, TUserId, TUserAuthenticatorId, TUserGroupId> : Entity<TId>
{
    public required TUserId UserId { get; set; }
    public required TUserGroupId GroupId { get; set; }

    public virtual User<TUserId, TUserAuthenticatorId>? User { get; set; }
    public virtual UserGroup<TUserGroupId, TUserId, TUserAuthenticatorId, TId>? Group { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
    public UserInGroup()
    {
        UserId = default!;
        GroupId = default!;
    }

    public UserInGroup(TUserId userId, TUserGroupId groupId)
    {
        UserId = userId;
        GroupId = groupId;
    }
}
