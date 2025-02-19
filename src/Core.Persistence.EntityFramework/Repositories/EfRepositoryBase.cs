using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

/// <summary>
/// Provides a base implementation for repository operations using Entity Framework.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TEntityId">The type of the entity identifier.</typeparam>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public partial class EfRepositoryBase<TEntity, TEntityId, TContext>(TContext context)
    : IAsyncRepository<TEntity, TEntityId>,
        IRepository<TEntity, TEntityId>,
        IQuery<TEntity>
    where TEntity : BaseEntity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext Context = context;

    /// <inheritdoc/>
    public IQueryable<TEntity> Query()
    {
        return Context.Set<TEntity>();
    }

    /// <inheritdoc/>
    public int SaveChanges()
    {
        return Context.SaveChanges();
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Standard messages for validation errors.
    /// </summary>
    protected struct Messages
    {
        public const string EntityCannotBeNull = "Entity cannot be null.";
        public const string CollectionCannotBeNull = "Collection cannot be null.";
        public const string CollectionContainsNullEntity = "Collection contains null entity.";
    }
}
