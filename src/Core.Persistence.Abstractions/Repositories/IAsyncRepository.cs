using System.Linq.Expressions;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

public interface IAsyncRepository<TEntity, TEntityId> : IQuery<TEntity>
    where TEntity : Entity<TEntityId>
{
    // Create Methods
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task BulkAddAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default);

    // Update Methods
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task BulkUpdateAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default);

    // Delete Methods
    Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    );

    // General Methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Read Methods
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );
    Task<TEntity?> GetByIdAsync(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        uint fetchLimit = 1_000_000,
        uint chunkSize = 1_000,
        CancellationToken cancellationToken = default
    );
    Task<IPaginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        uint index = 0,
        uint size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );
    Task<IPaginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        uint index = 0,
        uint size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsByIdAsync(TEntityId id, bool withDeleted = false, CancellationToken cancellationToken = default);
    Task<TEntity?> GetRandomAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );
    Task<IPaginate<TEntity>> GetRandomListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        uint index = 0,
        uint size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );
    Task<uint> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );
    Task<ulong> CountLongAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );
}
