using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected virtual void EditEntityPropertiesToAdd(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
    }

    public TEntity Add(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToAdd(entity);
        _ = Context.Add(entity);
        return entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), Messages.EntityCannotBeNull);

        EditEntityPropertiesToAdd(entity);
        _ = await Context.AddAsync(entity, cancellationToken);
        return entity;
    }

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
