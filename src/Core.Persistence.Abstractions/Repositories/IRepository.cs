using System.Linq.Expressions;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

public interface IRepository<TEntity, TEntityId> : IQuery<TEntity>
    where TEntity : Entity<TEntityId>
{
    // Create Methods
    TEntity Add(TEntity entity);
    void BulkAdd(ICollection<TEntity> entities, int batchSize = 1_000);

    // Update Methods
    TEntity Update(TEntity entity);
    void BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000);

    // Delete Methods
    TEntity Delete(TEntity entity, bool permanent = false);
    void BulkDelete(ICollection<TEntity> entities, bool permanent = false, int batchSize = 1_000);

    // General Methods
    int SaveChanges();

    // Read Methods
    TEntity? Get(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    );
    TEntity? GetById(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    );

    ICollection<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int fetchLimit = 1_000_000,
        int chunkSize = 1_000
    );
    IPaginate<TEntity> GetList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );
    IPaginate<TEntity> GetListByDynamic(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );

    bool ExistsById(TEntityId id, bool withDeleted = false);
    TEntity? GetRandom(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true);
    IPaginate<TEntity> GetRandomList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );
    int Count(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false);
    long CountLong(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false);
    bool Any(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false
    );
}
