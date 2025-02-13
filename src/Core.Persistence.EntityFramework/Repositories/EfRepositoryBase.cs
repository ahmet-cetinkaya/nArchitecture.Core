using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>(TContext context)
    : IAsyncRepository<TEntity, TEntityId>,
        IRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext Context = context;

    public IQueryable<TEntity> Query()
    {
        return Context.Set<TEntity>();
    }

    public int SaveChanges()
    {
        return Context.SaveChanges();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    protected struct Messages
    {
        public const string EntityCannotBeNull = "Entity cannot be null.";
        public const string CollectionCannotBeNull = "Collection cannot be null.";
        public const string CollectionContainsNullEntity = "Collection contains null entity.";
    }
}
