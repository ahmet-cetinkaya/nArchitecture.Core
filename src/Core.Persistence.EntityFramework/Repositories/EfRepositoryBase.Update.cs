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

        try
        {
            var (databaseEntity, _) = GetDatabaseValues(entity);
            ValidateEntityState(entity, databaseEntity);
            EditEntityPropertiesToUpdate(entity);
            Context.Entry(entity).State = EntityState.Modified;
            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            var (databaseEntity, _) = GetDatabaseValues(entity);
            ValidateEntityState(entity, databaseEntity);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        try
        {
            var (databaseEntity, _) = await GetDatabaseValuesAsync(entity, cancellationToken);
            ValidateEntityState(entity, databaseEntity);
            EditEntityPropertiesToUpdate(entity);
            Context.Entry(entity).State = EntityState.Modified;
            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            var (databaseEntity, _) = await GetDatabaseValuesAsync(entity, cancellationToken);
            ValidateEntityState(entity, databaseEntity);
            throw;
        }
    }

    /// <inheritdoc/>
    public ICollection<TEntity> BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return entities;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        try
        {
            foreach (TEntity entity in entities)
            {
                var (databaseEntity, _) = GetDatabaseValues(entity);
                ValidateEntityState(entity, databaseEntity);
                EditEntityPropertiesToUpdate(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
            return entities;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var failedEntity = entities.LastOrDefault(e => ex.Entries.Any(entry => entry.Entity == e));
            if (failedEntity != null)
            {
                var (databaseEntity, _) = GetDatabaseValues(failedEntity);
                ValidateEntityState(failedEntity, databaseEntity);
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ICollection<TEntity>> BulkUpdateAsync(
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

        try
        {
            foreach (TEntity entity in entities)
            {
                var (databaseEntity, _) = await GetDatabaseValuesAsync(entity, cancellationToken);
                ValidateEntityState(entity, databaseEntity);
                EditEntityPropertiesToUpdate(entity);
                Context.Entry(entity).State = EntityState.Modified;
            }
            return entities;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var failedEntity = entities.LastOrDefault(e => ex.Entries.Any(entry => entry.Entity == e));
            if (failedEntity != null)
            {
                var (databaseEntity, _) = await GetDatabaseValuesAsync(failedEntity, cancellationToken);
                ValidateEntityState(failedEntity, databaseEntity);
            }
            throw;
        }
    }
}
