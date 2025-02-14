using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    // Public Methods
    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        SetEntityAsDeleted(entity, permanent, isAsync: false).GetAwaiter().GetResult();
        return entity;
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
        return entity;
    }

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

    // Protected Methods
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

    protected virtual void EditEntityPropertiesToDelete(IEntityTimestamps entity, DateTime? deletionTime = null)
    {
        entity.DeletedDate = deletionTime ?? DateTime.UtcNow;
    }

    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedDate.HasValue;
    }

    private async Task cascadeSoftDelete(
        IEntityTimestamps entity,
        DateTime? deletionTime = null,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSoftDeleted(entity))
            return;

        deletionTime ??= DateTime.UtcNow;
        EditEntityPropertiesToDelete(entity, deletionTime);

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IEntityTimestamps> entry = Context.Entry(entity);
        var navigations = entry
            .Metadata.GetNavigations()
            .Cast<INavigationBase>()
            .Concat(entry.Metadata.GetSkipNavigations().Cast<INavigationBase>())
            .Where(x => !x.TargetEntityType.IsOwned())
            .ToList();

        foreach (INavigationBase? navigation in navigations)
        {
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = null;

            if (navigation.IsCollection)
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.CollectionEntry collectionEntry = entry.Collection(navigation.PropertyInfo.Name);
                if (!collectionEntry.IsLoaded)
                    await collectionEntry.LoadAsync(cancellationToken);
                navValue = collectionEntry.CurrentValue;
            }
            else
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.ReferenceEntry referenceEntry = entry.Reference(navigation.PropertyInfo.Name);
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

        _ = Context.Update(entity);
    }
}
