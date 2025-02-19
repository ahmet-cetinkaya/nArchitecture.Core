using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.Abstractions.Authenticator;

public interface IAuthenticator<TUserId, TUserAuthenticatorId>
{
    Task<UserAuthenticator<TUserAuthenticatorId, TUserId>> CreateAsync(
        TUserId userId,
        AuthenticatorType type,
        string? destination,
        CancellationToken cancellationToken = default
    );
    Task VerifyAsync(TUserId userId, string code, CancellationToken cancellationToken = default);
    Task AttemptAsync(TUserId userId, string? destination, CancellationToken cancellationToken = default);
    Task DeleteAsync(TUserId userId, CancellationToken cancellationToken = default);
}
