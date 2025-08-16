using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : BaseEntity<TEntityId>
    where TContext : DbContext
{
    /// <inheritdoc/>
    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        try
        {
            (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) = GetDatabaseValues(entity);
            ValidateEntityState(entity, databaseEntity, ignoreSoftDelete: permanent);

            if (permanent)
                _ = Context.Remove(entity);
            else
            {
                SetEntityAsDeleted(entity, permanent, isAsync: false).GetAwaiter().GetResult();
                Context.Entry(entity).State = EntityState.Modified;
            }

            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) = GetDatabaseValues(entity);
            ValidateEntityState(entity, databaseEntity, ignoreSoftDelete: permanent);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        try
        {
            (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) =
                await GetDatabaseValuesAsync(entity, cancellationToken);
            ValidateEntityState(entity, databaseEntity, ignoreSoftDelete: permanent);

            if (permanent)
                _ = Context.Remove(entity);
            else
            {
                await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
                Context.Entry(entity).State = EntityState.Modified;
            }

            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) =
                await GetDatabaseValuesAsync(entity, cancellationToken);
            ValidateEntityState(entity, databaseEntity, ignoreSoftDelete: permanent);
            throw;
        }
    }

    /// <inheritdoc/>
    public ICollection<TEntity> BulkDelete(ICollection<TEntity> entities, bool permanent = false, int batchSize = 1_000)
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
                (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) = GetDatabaseValues(
                    entity
                );
                ValidateEntityState(entity, databaseEntity);

                if (permanent)
                    _ = Context.Remove(entity);
                else
                {
                    SetEntityAsDeleted(entity, permanent, isAsync: false).GetAwaiter().GetResult();
                    Context.Entry(entity).State = EntityState.Modified;
                }
            }

            return entities;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            TEntity? failedEntity = entities.LastOrDefault(e => ex.Entries.Any(entry => entry.Entity == e));
            if (failedEntity != null)
            {
                (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) = GetDatabaseValues(
                    failedEntity
                );
                ValidateEntityState(failedEntity, databaseEntity);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ICollection<TEntity>> BulkDeleteAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
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
                (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) =
                    await GetDatabaseValuesAsync(entity, cancellationToken);
                ValidateEntityState(entity, databaseEntity);

                if (permanent)
                    _ = Context.Remove(entity);
                else
                {
                    await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
                    Context.Entry(entity).State = EntityState.Modified;
                }
            }

            return entities;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            TEntity? failedEntity = entities.LastOrDefault(e => ex.Entries.Any(entry => entry.Entity == e));
            if (failedEntity != null)
            {
                (TEntity databaseEntity, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues _) =
                    await GetDatabaseValuesAsync(failedEntity, cancellationToken);
                ValidateEntityState(failedEntity, databaseEntity);
            }

            throw;
        }
    }

    /// <summary>
    /// Sets the entity as deleted. For soft deletion, cascades the delete to related entities.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="permanent">If set to true, performs a hard delete.</param>
    /// <param name="isAsync">Determines if the operation is asynchronous.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    protected async Task SetEntityAsDeleted(
        TEntity entity,
        bool permanent,
        bool isAsync = true,
        CancellationToken cancellationToken = default
    )
    {
        if (permanent)
        {
            _ = Context.Remove(entity);
        }
        else
        {
            if (isAsync)
                await cascadeSoftDelete(entity, cancellationToken: cancellationToken);
            else
                cascadeSoftDelete(entity, cancellationToken: cancellationToken).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Updates deletion-related properties for soft delete.
    /// </summary>
    /// <param name="entity">Entity that implements IEntityTimestamps.</param>
    /// <param name="deletionTime">Optional deletion time.</param>
    protected virtual void EditEntityPropertiesToDelete(IEntityTimestamps entity, DateTime? deletionTime = null)
    {
        entity.DeletedAt = deletionTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Determines whether the entity is already soft-deleted.
    /// </summary>
    /// <param name="entity">Entity that implements IEntityTimestamps.</param>
    /// <returns>True if soft-deleted; otherwise, false.</returns>
    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedAt.HasValue;
    }

    /// <summary>
    /// Recursively cascades soft deletion to navigated entities.
    /// </summary>
    /// <param name="entity">Entity to perform cascade delete on.</param>
    /// <param name="deletionTime">Optional deletion time.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    private async Task cascadeSoftDelete(
        IEntityTimestamps entity,
        DateTime? deletionTime = null,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSoftDeleted(entity))
            return;

        // Update deletion-related properties.
        deletionTime ??= DateTime.UtcNow;
        EditEntityPropertiesToDelete(entity, deletionTime);

        // Retrieve navigation properties.
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IEntityTimestamps> entry = Context.Entry(entity);
        var navigations = entry
            .Metadata.GetNavigations()
            .Cast<INavigationBase>()
            .Concat(entry.Metadata.GetSkipNavigations().Cast<INavigationBase>())
            .Where(x => !x.TargetEntityType.IsOwned())
            .ToList();

        // Cascade soft delete to navigated entities.
        foreach (INavigationBase? navigation in navigations)
        {
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = null;

            if (navigation.IsCollection)
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.CollectionEntry collectionEntry = entry.Collection(
                    navigation.PropertyInfo.Name
                );
                if (!collectionEntry.IsLoaded)
                    await collectionEntry.LoadAsync(cancellationToken);
                navValue = collectionEntry.CurrentValue;
            }
            else
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.ReferenceEntry referenceEntry = entry.Reference(
                    navigation.PropertyInfo.Name
                );
                if (!referenceEntry.IsLoaded)
                    await referenceEntry.LoadAsync(cancellationToken);
                navValue = referenceEntry.CurrentValue;
            }

            if (navValue == null)
                continue;

            if (navigation.IsCollection)
            {
                foreach (object? navItem in (IEnumerable)navValue)
                    if (navItem is IEntityTimestamps entityWithTimestamps)
                        await cascadeSoftDelete(entityWithTimestamps, deletionTime, cancellationToken);
            }
            else
            {
                if (navValue is IEntityTimestamps entityWithTimestamps)
                    await cascadeSoftDelete(entityWithTimestamps, deletionTime, cancellationToken);
            }
        }

        // Update entity state.
        _ = Context.Update(entity);
    }
}
