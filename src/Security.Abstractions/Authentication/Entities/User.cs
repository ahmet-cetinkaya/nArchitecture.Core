using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authentication.Entities;

public class User<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserInGroupId,
    TUserOperationClaimId
> : BaseEntity<TId>
{
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public User()
    {
        PasswordSalt = default!;
        PasswordHash = default!;
    }

    public User(byte[] passwordSalt, byte[] passwordHash)
    {
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
    }
}
