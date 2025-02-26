using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authentication.Entities;

public class RefreshToken<
    TId,
    TOperationClaimId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
> : BaseEntity<TId>
{
    public TUserId UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public RefreshToken()
    {
        UserId = default!;
        Token = default!;
        ExpiresAt = default!;
        CreatedByIp = default!;
    }

    public RefreshToken(TUserId userId, string token, DateTime expiresAt, string createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public virtual User<
        TUserId,
        TOperationClaimId,
        TId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserInGroupId,
        TUserOperationClaimId
    >? User { get; set; }
}
