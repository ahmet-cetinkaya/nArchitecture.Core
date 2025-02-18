using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authentication.Entities;

public class User<TId, TUserAuthenticatorId> : Entity<TId>
{
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }

    public ICollection<UserAuthenticator<TUserAuthenticatorId, TId>>? Authenticators { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
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
