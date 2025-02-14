using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    /// <summary>
    /// Sets the creation timestamp on the entity.
    /// </summary>
    protected virtual void EditEntityPropertiesToAdd(TEntity entity)
    {
        // Set current UTC time as creation time.
        entity.CreatedDate = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public TEntity Add(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToAdd(entity);
        _ = Context.Add(entity);
        return entity;
    }

    /// <inheritdoc/>
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToAdd(entity);
        _ = await Context.AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual void BulkAdd(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);
            EditEntityPropertiesToAdd(entity);
            _ = Context.Add(entity);
        }
    }

    /// <inheritdoc/>
    public virtual async Task BulkAddAsync(
        ICollection<TEntity> entities,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);
            EditEntityPropertiesToAdd(entity);
            _ = await Context.AddAsync(entity, cancellationToken);
        }
    }
}
