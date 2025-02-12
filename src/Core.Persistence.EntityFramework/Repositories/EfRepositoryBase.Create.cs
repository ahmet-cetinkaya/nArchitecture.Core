using EFCore.BulkExtensions;
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
        EditEntityPropertiesToAdd(entity);
        _ = Context.Add(entity);
        return entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EditEntityPropertiesToAdd(entity);
        _ = await Context.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void BulkAdd(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
            EditEntityPropertiesToAdd(entity);

        foreach (var batch in entities.Chunk(batchSize))
        {
            Context.BulkInsert(batch);
        }
    }

    public async Task BulkAddAsync(
        ICollection<TEntity> entities,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
            EditEntityPropertiesToAdd(entity);

        foreach (var batch in entities.Chunk(batchSize))
        {
            await Context.BulkInsertAsync(batch, cancellationToken: cancellationToken);
        }
    }
}
