using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected virtual void EditEntityPropertiesToUpdate(TEntity entity)
    {
        entity.UpdatedDate = DateTime.UtcNow;
    }

    public TEntity Update(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return entity;
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return Task.FromResult(entity);
    }

    public void BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000)
    {
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

    public Task BulkUpdateAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default)
    {
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
