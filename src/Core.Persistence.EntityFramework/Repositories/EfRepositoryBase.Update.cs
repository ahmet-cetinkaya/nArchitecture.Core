using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    /// <summary>
    /// Updates the entity's update timestamp.
    /// </summary>
    protected virtual void EditEntityPropertiesToUpdate(TEntity entity)
    {
        // Set current UTC time as update time.
        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public TEntity Update(TEntity entity)
    {
        // Validate input.
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return entity;
    }

    /// <inheritdoc/>
    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // Validate input.
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc/>
    public void BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        // Validate input collection.
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToUpdate(entity);
            _ = Context.Update(entity);
        }
    }

    /// <inheritdoc/>
    public Task BulkUpdateAsync(
        ICollection<TEntity> entities,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        // Validate input collection.
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return Task.CompletedTask;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToUpdate(entity);
            _ = Context.Update(entity);
        }

        return Task.CompletedTask;
    }
}
