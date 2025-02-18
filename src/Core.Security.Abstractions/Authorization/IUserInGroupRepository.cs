using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

public interface IUserInGroupRepository<TId, TUserId, TUserAuthenticatorId, TUserGroupId>
    : IAsyncRepository<UserInGroup<TId, TUserId, TUserAuthenticatorId, TUserGroupId>, TId> { }
