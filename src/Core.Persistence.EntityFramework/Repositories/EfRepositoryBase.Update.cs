using EFCore.BulkExtensions;
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
        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return entity;
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EditEntityPropertiesToUpdate(entity);
        _ = Context.Update(entity);
        return Task.FromResult(entity);
    }

    public void BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000)
    {
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
            EditEntityPropertiesToUpdate(entity);

        foreach (var batch in entities.Chunk(batchSize))
        {
            Context.BulkUpdate(batch);
        }
    }

    public async Task BulkUpdateAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default)
    {
        if (entities.Count == 0)
            return;

        foreach (TEntity entity in entities)
            EditEntityPropertiesToUpdate(entity);

        foreach (var batch in entities.Chunk(batchSize))
        {
            await Context.BulkUpdateAsync(batch, cancellationToken: cancellationToken);
        }
    }
}
