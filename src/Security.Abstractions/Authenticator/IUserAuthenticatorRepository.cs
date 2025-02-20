using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authenticator;

public interface IUserAuthenticatorRepository<TUserId, TUserAuthenticatorId>
    : IAsyncRepository<UserAuthenticator<TUserAuthenticatorId, TUserId>, TUserAuthenticatorId> { }
