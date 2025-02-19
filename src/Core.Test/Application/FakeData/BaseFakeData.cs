using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Test.Application.FakeData;

/// <summary>
/// Base class for creating fake data for testing purposes.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TEntityId">The entity id type</typeparam>
public abstract class BaseFakeData<TEntity, TEntityId>
    where TEntity : BaseEntity<TEntityId>, new()
{
    /// <summary>
    /// Gets the fake data list for testing.
    /// </summary>
    public List<TEntity> Data => CreateFakeData();

    /// <summary>
    /// Creates a list of fake entities for testing.
    /// </summary>
    /// <returns>List of fake entities</returns>
    public abstract List<TEntity> CreateFakeData();
}
