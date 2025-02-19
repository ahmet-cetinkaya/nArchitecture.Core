using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

public interface IUserGroupRepository<TId, TUserId, TUserAuthenticatorId, TUserInGroupId>
    : IAsyncRepository<UserGroup<TId, TUserId, TUserAuthenticatorId, TUserInGroupId>, TId> { }
