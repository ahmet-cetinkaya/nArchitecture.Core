using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : BaseEntity<TEntityId>
    where TContext : DbContext
{
    /// <summary>
    /// Sets required properties before adding the entity to the database.
    /// Currently sets CreatedAt to current UTC time.
    /// </summary>
    protected virtual void EditEntityPropertiesToAdd(TEntity entity)
    {
        // Set current UTC time as creation time.
        entity.CreatedAt = DateTime.UtcNow;
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
    public virtual ICollection<TEntity> BulkAdd(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return entities;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToAdd(entity);
            _ = Context.Add(entity);
        }

        return entities;
    }

    /// <inheritdoc/>
    public virtual async Task<ICollection<TEntity>> BulkAddAsync(
        ICollection<TEntity> entities,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return entities;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToAdd(entity);
            _ = await Context.AddAsync(entity, cancellationToken);
        }

        return entities;
    }
}
