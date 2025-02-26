using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

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

    public ICollection<
        UserAuthenticator<
            TUserAuthenticatorId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserGroupId,
            TId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    >? UserAuthenticators { get; set; }
    public ICollection<
        RefreshToken<
            TRefreshTokenId,
            TOperationClaimId,
            TUserAuthenticatorId,
            TUserGroupId,
            TId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    >? RefreshTokens { get; set; }
    public ICollection<
        UserOperationClaim<
            TUserOperationClaimId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserAuthenticatorId,
            TUserGroupId,
            TId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    >? UserOperationClaims { get; set; }
    public ICollection<
        UserInGroup<
            TUserInGroupId,
            TOperationClaimId,
            TRefreshTokenId,
            TUserAuthenticatorId,
            TUserGroupId,
            TId,
            TUserInGroupId,
            TUserOperationClaimId
        >
    >? UserInGroups { get; set; }
}
