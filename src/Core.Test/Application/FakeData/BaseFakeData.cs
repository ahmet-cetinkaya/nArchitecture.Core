using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Test.Application.FakeData;

public abstract class BaseFakeData<TEntity, TEntityId>
    where TEntity : BaseEntity<TEntityId>, new()
{
    public List<TEntity> Data => CreateFakeData();
    public abstract List<TEntity> CreateFakeData();
}
