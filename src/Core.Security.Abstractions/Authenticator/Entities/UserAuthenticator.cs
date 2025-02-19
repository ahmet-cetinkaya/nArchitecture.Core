using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.Abstractions.Authenticator.Entities;

public class UserAuthenticator<TId, TUserId> : BaseEntity<TId>
{
    public TUserId UserId { get; set; }
    public AuthenticatorType Type { get; set; }
    public byte[]? CodeSeed { get; set; }
    public string? Code { get; set; }
    public DateTime? CodeExpiresAt { get; set; }
    public bool IsVerified { get; set; }

    public virtual User<TUserId, TId>? User { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
    public UserAuthenticator()
    {
        UserId = default!;
        Type = default!;
        CodeSeed = default!;
        Code = default!;
        CodeExpiresAt = default!;
        IsVerified = default!;
    }

    public UserAuthenticator(TUserId userId, AuthenticatorType type)
    {
        UserId = userId;
        Type = type;
    }
}
