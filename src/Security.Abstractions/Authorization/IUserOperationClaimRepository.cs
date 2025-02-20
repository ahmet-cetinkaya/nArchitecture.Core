using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

public interface IUserOperationClaimRepository<TId, TUserId, TOperationClaimId>
    : IAsyncRepository<UserOperationClaim<TId, TUserId, TOperationClaimId>, TId> { }
