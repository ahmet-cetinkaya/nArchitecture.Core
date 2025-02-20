using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Security.Abstractions.Authorization.Entities;

public class OperationClaim<TId> : BaseEntity<TId>
{
    public string Name { get; set; }

    [Obsolete("This constructor is for ORM etc.. Do not use it in the code.", true)]
    public OperationClaim()
    {
        Name = default!;
    }

    public OperationClaim(string name)
    {
        Name = name;
    }
}
