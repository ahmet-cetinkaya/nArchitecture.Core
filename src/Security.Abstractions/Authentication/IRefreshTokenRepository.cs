using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication;

public interface IRefreshTokenRepository<TId, TUserId, TUserAuthenticatorId>
    : IAsyncRepository<RefreshToken<TId, TUserId, TUserAuthenticatorId>, TId>
{
    Task<RefreshToken<TId, TUserId, TUserAuthenticatorId>?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default
    );
    Task<ICollection<RefreshToken<TId, TUserId, TUserAuthenticatorId>>> GetAllActiveByUserIdAsync(
        TUserId userId,
        CancellationToken cancellationToken = default
    );
}
