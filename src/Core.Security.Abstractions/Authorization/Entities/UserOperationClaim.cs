using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class UserOperationClaim<TId, TUserId, TOperationClaimId> : BaseEntity<TId>
{
    public TUserId UserId { get; set; }
    public TOperationClaimId OperationClaimId { get; set; }

    public virtual User<TId, TUserId>? User { get; set; }
    public virtual OperationClaim<TOperationClaimId>? OperationClaim { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
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
}
