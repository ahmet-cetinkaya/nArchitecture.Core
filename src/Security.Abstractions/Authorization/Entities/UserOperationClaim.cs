using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserOperationClaim<
    TId,
    TOperationClaimId,
    TRefreshTokenId,
    TUserAuthenticatorId,
    TUserGroupId,
    TUserId,
    TUserInGroupId,
    TUserOperationClaimId
> : BaseEntity<TId>
{
    public TUserId UserId { get; set; }
    public TOperationClaimId OperationClaimId { get; set; }

    [Obsolete("This constructor is for ORM, mapper etc.. Do not use it in the code.", true)]
    public UserOperationClaim()
    {
        UserId = default!;
        OperationClaimId = default!;
    }

    public UserOperationClaim(TUserId userId, TOperationClaimId operationClaimId)
    {
        UserId = userId;
        OperationClaimId = operationClaimId;
    }

    public virtual User<
        TUserId,
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserInGroupId,
        TUserOperationClaimId
    >? User { get; set; }
    public virtual OperationClaim<TOperationClaimId>? OperationClaim { get; set; }
}
