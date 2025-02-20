using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;

namespace NArchitecture.Core.Security.Abstractions.Authorization;

public interface IOperationClaimRepository<TOperationClaimId>
    : IAsyncRepository<OperationClaim<TOperationClaimId>, TOperationClaimId> { }
