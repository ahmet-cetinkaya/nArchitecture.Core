using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    /// <inheritdoc/>
    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        // Delete synchronously with cascade soft-delete if not permanent.
        SetEntityAsDeleted(entity, permanent, isAsync: false).GetAwaiter().GetResult();
        return entity;
    }

    /// <inheritdoc/>
    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        // Delete asynchronously with cascade logic.
        await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public void BulkDelete(ICollection<TEntity> entities, bool permanent = false, int batchSize = 1_000)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            if (permanent)
                _ = Context.Remove(entity);
            else
            {
                SetEntityAsDeleted(entity, permanent, isAsync: false).GetAwaiter().GetResult();
                _ = Context.Update(entity);
            }
        }
    }

    /// <inheritdoc/>
    public async Task BulkDeleteAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities), Messages.CollectionCannotBeNull);
        if (entities.Count == 0)
            return;
        if (entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities), Messages.CollectionContainsNullEntity);

        foreach (TEntity entity in entities)
        {
            if (permanent)
                _ = Context.Remove(entity);
            else
            {
                await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
                _ = Context.Update(entity);
            }
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
        entity.DeletedDate = deletionTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Determines whether the entity is already soft-deleted.
    /// </summary>
    /// <param name="entity">Entity that implements IEntityTimestamps.</param>
    /// <returns>True if soft-deleted; otherwise, false.</returns>
    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedDate.HasValue;
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
        var entry = Context.Entry(entity);
        var navigations = entry
            .Metadata.GetNavigations()
            .Cast<INavigationBase>()
            .Concat(entry.Metadata.GetSkipNavigations().Cast<INavigationBase>())
            .Where(x => !x.TargetEntityType.IsOwned())
            .ToList();

        // Cascade soft delete to navigated entities.
        foreach (var navigation in navigations)
        {
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = null;

            if (navigation.IsCollection)
            {
                var collectionEntry = entry.Collection(navigation.PropertyInfo.Name);
                if (!collectionEntry.IsLoaded)
                    await collectionEntry.LoadAsync(cancellationToken);
                navValue = collectionEntry.CurrentValue;
            }
            else
            {
                var referenceEntry = entry.Reference(navigation.PropertyInfo.Name);
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
