using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : BaseEntity<TEntityId>
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
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        var databaseEntity = GetAndCheckEntityStatus(entity);
        if (databaseEntity.UpdatedAt != entity.UpdatedAt)
            throw new DbUpdateConcurrencyException(
                $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
            );

        EditEntityPropertiesToUpdate(entity);
        Context.Entry(entity).State = EntityState.Modified;
        return entity;
    }

    /// <inheritdoc/>
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        var databaseEntity = await GetAndCheckEntityStatusAsync(entity, cancellationToken);
        if (databaseEntity.UpdatedAt != entity.UpdatedAt)
            throw new DbUpdateConcurrencyException(
                $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
            );

        EditEntityPropertiesToUpdate(entity);
        Context.Entry(entity).State = EntityState.Modified;
        return entity;
    }

    /// <inheritdoc/>
    public ICollection<TEntity> BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        // Validate input collection.
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return entities;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToUpdate(entity);
            _ = Context.Update(entity);
        }

        return entities;
    }

    /// <inheritdoc/>
    public Task<ICollection<TEntity>> BulkUpdateAsync(
        ICollection<TEntity> entities,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        // Validate input collection.
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return Task.FromResult<ICollection<TEntity>>(entities);
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            EditEntityPropertiesToUpdate(entity);
            _ = Context.Update(entity);
        }

        return Task.FromResult<ICollection<TEntity>>(entities);
    }
}
