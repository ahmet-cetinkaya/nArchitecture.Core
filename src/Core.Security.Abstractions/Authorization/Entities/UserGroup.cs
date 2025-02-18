using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserGroup<TId, TUserId, TUserAuthenticatorId, TUserInGroupId> : Entity<TId>
{
    public string Name { get; set; }

    public virtual ICollection<
        UserGroupOperationClaim<TId, TId, TUserId, TUserAuthenticatorId, TUserInGroupId, TId>
    >? GroupOperationClaims { get; set; }
    public virtual ICollection<UserInGroup<TUserInGroupId, TUserId, TUserAuthenticatorId, TId>>? UserInGroups { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
    public UserGroup()
    {
        Name = default!;
    }

    public UserGroup(string name)
    {
        Name = name;
    }
}
