using System.Linq.Expressions;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;

namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Defines repository operations for performing create, read, update, and delete actions on entities.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TEntityId">The type of the entity identifier.</typeparam>
public interface IRepository<TEntity, TEntityId> : IQuery<TEntity>
    where TEntity : BaseEntity<TEntityId>
{
    #region Create Methods
    /// <summary>
    /// Adds the specified entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    TEntity Add(TEntity entity);

    /// <summary>
    /// Adds a collection of entities in bulk.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <returns>The collection of added entities.</returns>
    ICollection<TEntity> BulkAdd(ICollection<TEntity> entities, int batchSize = 1_000);
    #endregion

    #region Update Methods
    /// <summary>
    /// Updates the specified entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    TEntity Update(TEntity entity);

    /// <summary>
    /// Updates a collection of entities in bulk.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <returns>The collection of updated entities.</returns>
    ICollection<TEntity> BulkUpdate(ICollection<TEntity> entities, int batchSize = 1_000);
    #endregion

    #region Delete Methods
    /// <summary>
    /// Deletes the specified entity.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="permanent">if set to <c>true</c> [permanent].</param>
    /// <returns>The deleted entity.</returns>
    TEntity Delete(TEntity entity, bool permanent = false);

    /// <summary>
    /// Deletes a collection of entities in bulk.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="permanent">if set to <c>true</c> [permanent].</param>
    /// <param name="batchSize">The batch size for bulk operations.</param>
    /// <returns>The collection of deleted entities.</returns>
    ICollection<TEntity> BulkDelete(ICollection<TEntity> entities, bool permanent = false, int batchSize = 1_000);
    #endregion

    #region General Methods
    /// <summary>
    /// Commits all pending changes by writing new, updated, and deleted entities to the database.
    /// </summary>
    /// <remarks>
    /// When invoked, this method processes all modifications made in the context,
    /// ensuring that all changes (inserts, updates, and deletes) are persisted to the persistent store.
    /// </remarks>
    /// <returns>The number of state entries written to the database.</returns>
    int SaveChanges();
    #endregion

    #region Read Methods
    /// <summary>
    /// Gets the entity that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The entity that matches the predicate.</returns>
    TEntity? Get(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    );

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The entity that matches the identifier.</returns>
    TEntity? GetById(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    );

    /// <summary>
    /// Gets all entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <param name="fetchLimit">The fetch limit.</param>
    /// <param name="chunkSize">Size of the chunk.</param>
    /// <returns>The collection of entities that match the predicate.</returns>
    ICollection<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int fetchLimit = 1_000_000,
        int chunkSize = 1_000
    );

    /// <summary>
    /// Gets the list of entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="orderBy">The order by function to order entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index.</param>
    /// <param name="size">The size.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The paginated list of entities that match the predicate.</returns>
    IPaginate<TEntity> GetList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );

    /// <summary>
    /// Gets the list of entities that match the specified dynamic query.
    /// </summary>
    /// <param name="dynamic">The dynamic query.</param>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index.</param>
    /// <param name="size">The size.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The paginated list of entities that match the dynamic query.</returns>
    IPaginate<TEntity> GetListByDynamic(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );

    /// <summary>
    /// Determines whether an entity exists by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    bool ExistsById(TEntityId id, bool withDeleted = false);

    /// <summary>
    /// Gets a random entity that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The random entity that matches the predicate.</returns>
    TEntity? GetRandom(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true);

    /// <summary>
    /// Gets a random list of entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="index">The index.</param>
    /// <param name="size">The size.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <param name="enableTracking">if set to <c>true</c> [enable tracking].</param>
    /// <returns>The paginated list of random entities that match the predicate.</returns>
    IPaginate<TEntity> GetRandomList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    );

    /// <summary>
    /// Counts the number of entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <returns>The number of entities that match the predicate.</returns>
    int Count(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false);

    /// <summary>
    /// Counts the number of entities that match the specified predicate as a long value.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <returns>The number of entities that match the predicate as a long value.</returns>
    long CountLong(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false);

    /// <summary>
    /// Determines whether any entity matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="include">The include function to include related entities.</param>
    /// <param name="withDeleted">if set to <c>true</c> [with deleted].</param>
    /// <returns><c>true</c> if any entity matches the predicate; otherwise, <c>false</c>.</returns>
    bool Any(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false
    );
    #endregion
}
