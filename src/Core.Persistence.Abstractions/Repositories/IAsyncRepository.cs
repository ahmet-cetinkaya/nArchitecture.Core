using System.Linq.Expressions;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Defines asynchronous repository operations for performing CRUD actions on entities.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TEntityId">The type of the entity identifier.</typeparam>
public interface IAsyncRepository<TEntity, TEntityId> : IQuery<TEntity>
    where TEntity : BaseEntity<TEntityId>
{
    #region Create Methods
    /// <summary>
    /// Adds an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of entities asynchronously in bulk.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BulkAddAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default);
    #endregion

    #region Update Methods
    /// <summary>
    /// Updates an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a collection of entities asynchronously in bulk.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BulkUpdateAsync(ICollection<TEntity> entities, int batchSize = 1_000, CancellationToken cancellationToken = default);
    #endregion

    #region Delete Methods
    /// <summary>
    /// Deletes an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="permanent">Indicates whether the deletion is permanent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deleted entity.</returns>
    Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a collection of entities asynchronously in bulk.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="permanent">Indicates whether the deletion is permanent.</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task BulkDeleteAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    );
    #endregion

    #region General Methods
    /// <summary>
    /// Asynchronously commits all pending changes by writing new, updated, and deleted entities to the database.
    /// </summary>
    /// <remarks>
    /// When invoked, this method asynchronously processes all modifications made in the DbContext,
    /// ensuring that all changes (inserts, updates, and deletes) are persisted to the underlying database.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    #endregion

    #region Read Methods
    /// <summary>
    /// Gets an entity asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entity.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity that matches the predicate.</returns>
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets an entity by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity that matches the identifier.</returns>
    Task<TEntity?> GetByIdAsync(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all entities asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="fetchLimit">The fetch limit for the query.</param>
    /// <param name="chunkSize">The chunk size for the query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The collection of entities that match the predicate.</returns>
    Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int fetchLimit = 1_000_000,
        int chunkSize = 1_000,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a paginated list of entities asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="orderBy">The order by function to order the entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index of the page.</param>
    /// <param name="size">The size of the page.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paginated list of entities that match the predicate.</returns>
    Task<IPaginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a paginated list of entities asynchronously based on a dynamic query.
    /// </summary>
    /// <param name="dynamic">The dynamic query.</param>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index of the page.</param>
    /// <param name="size">The size of the page.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paginated list of entities that match the dynamic query.</returns>
    Task<IPaginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if an entity exists by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the entity exists, otherwise false.</returns>
    Task<bool> ExistsByIdAsync(TEntityId id, bool withDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a random entity asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entity.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The random entity that matches the predicate.</returns>
    Task<TEntity?> GetRandomAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a paginated list of random entities asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index of the page.</param>
    /// <param name="size">The size of the page.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="enableTracking">Indicates whether to enable tracking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paginated list of random entities that match the predicate.</returns>
    Task<IPaginate<TEntity>> GetRandomListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Counts the number of entities asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of entities that match the predicate.</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Counts the number of entities asynchronously based on a predicate and returns a long value.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of entities that match the predicate as a long value.</returns>
    Task<long> CountLongAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if any entity exists asynchronously based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter the entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">Indicates whether to include deleted entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entity exists, otherwise false.</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    );
    #endregion
}
