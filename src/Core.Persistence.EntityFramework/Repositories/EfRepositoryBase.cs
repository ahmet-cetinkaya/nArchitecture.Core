using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

/// <summary>
/// Base implementation for Entity Framework Core based repositories that provides CRUD and query operations.
/// Implements optimistic concurrency control using RowVersion and UpdatedAt properties.
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
/// <typeparam name="TEntityId">The type of the entity's primary key</typeparam>
/// <typeparam name="TContext">The Entity Framework DbContext type</typeparam>
public partial class EfRepositoryBase<TEntity, TEntityId, TContext>(TContext context)
    : IAsyncRepository<TEntity, TEntityId>,
        IRepository<TEntity, TEntityId>,
        IQuery<TEntity>
    where TEntity : BaseEntity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext Context = context;

    /// <inheritdoc/>
    public IQueryable<TEntity> Query()
    {
        return Context.Set<TEntity>();
    }

    /// <inheritdoc/>
    public int SaveChanges()
    {
        try 
        {
            return Context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            HandleAnyConcurrencyException(ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try 
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await HandleAnyConcurrencyExceptionAsync(ex, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Gets the entity values from the database and checks its status.
    /// </summary>
    protected virtual async Task<TEntity> GetAndCheckEntityStatusAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        var entry = Context.Entry(entity);
        var databaseValues =
            await entry.GetDatabaseValuesAsync(cancellationToken)
            ?? throw new InvalidOperationException($"The entity with id {entity.Id} no longer exists in the database.");

        var databaseEntity = (TEntity)databaseValues.ToObject();

        if (databaseEntity.DeletedAt.HasValue)
            throw new InvalidOperationException($"The entity with id {entity.Id} has been deleted by another user.");

        // Check both RowVersion and UpdatedAt for concurrency
        if (!databaseEntity.RowVersion.SequenceEqual(entity.RowVersion) || databaseEntity.UpdatedAt != entity.UpdatedAt)
            throw new DbUpdateConcurrencyException(
                $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
            );

        return databaseEntity;
    }

    /// <summary>
    /// Gets the entity values from the database and checks its status.
    /// </summary>
    protected virtual TEntity GetAndCheckEntityStatus(TEntity entity)
    {
        var entry = Context.Entry(entity);
        var databaseValues =
            entry.GetDatabaseValues()
            ?? throw new InvalidOperationException($"The entity with id {entity.Id} no longer exists in the database.");

        var databaseEntity = (TEntity)databaseValues.ToObject();

        if (databaseEntity.DeletedAt.HasValue)
            throw new InvalidOperationException($"The entity with id {entity.Id} has been deleted by another user.");

        // Check both RowVersion and UpdatedAt for concurrency
        if (!databaseEntity.RowVersion.SequenceEqual(entity.RowVersion) || databaseEntity.UpdatedAt != entity.UpdatedAt)
            throw new DbUpdateConcurrencyException(
                $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
            );

        return databaseEntity;
    }

    /// <summary>
    /// Handles the concurrency exception by checking entity status and throwing appropriate exception.
    /// </summary>
    protected virtual void HandleConcurrencyException(DbUpdateConcurrencyException ex, TEntity entity)
    {
        try
        {
            var databaseEntity = GetAndCheckEntityStatus(entity);

            if (databaseEntity.UpdatedAt != entity.UpdatedAt)
                throw new DbUpdateConcurrencyException(
                    $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
                );
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw if entity not found or deleted
        }
    }

    /// <summary>
    /// Handles the concurrency exception asynchronously by checking entity status and throwing appropriate exception.
    /// </summary>
    protected virtual async Task HandleConcurrencyExceptionAsync(
        DbUpdateConcurrencyException ex,
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var databaseEntity = await GetAndCheckEntityStatusAsync(entity, cancellationToken);

            if (databaseEntity.UpdatedAt != entity.UpdatedAt)
                throw new DbUpdateConcurrencyException(
                    $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
                );
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw if entity not found or deleted
        }
    }

    protected virtual void HandleAnyConcurrencyException(DbUpdateConcurrencyException ex)
    {
        foreach (var entry in ex.Entries)
        {
            if (entry.Entity is TEntity entity)
            {
                var (databaseEntity, _) = GetDatabaseValues(entity);
                ValidateEntityState(entity, databaseEntity);
                throw new DbUpdateConcurrencyException(
                    $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
                );
            }
        }
    }

    protected virtual async Task HandleAnyConcurrencyExceptionAsync(
        DbUpdateConcurrencyException ex,
        CancellationToken cancellationToken
    )
    {
        foreach (var entry in ex.Entries)
        {
            if (entry.Entity is TEntity entity)
            {
                var (databaseEntity, _) = await GetDatabaseValuesAsync(entity, cancellationToken);
                ValidateEntityState(entity, databaseEntity);
                throw new DbUpdateConcurrencyException(
                    $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
                );
            }
        }
    }

    /// <summary>
    /// Gets the entity's current database values and validates its state.
    /// Throws appropriate exceptions if the entity is deleted or modified by another user.
    /// </summary>
    /// <returns>Tuple containing the database entity and its property values</returns>
    protected virtual (TEntity DatabaseEntity, PropertyValues Values) GetDatabaseValues(TEntity entity)
    {
        var entry = Context.Entry(entity);
        var values = entry.GetDatabaseValues() 
            ?? throw new InvalidOperationException($"The entity with id {entity.Id} no longer exists in the database.");
        return ((TEntity)values.ToObject(), values);
    }

    /// <summary>
    /// Asynchronously gets the entity's current database values and validates its state.
    /// Throws appropriate exceptions if the entity is deleted or modified by another user.
    /// </summary>
    /// <returns>Tuple containing the database entity and its property values</returns>
    protected virtual async Task<(TEntity DatabaseEntity, PropertyValues Values)> GetDatabaseValuesAsync(
        TEntity entity,
        CancellationToken cancellationToken
    )
    {
        var entry = Context.Entry(entity);
        var values = await entry.GetDatabaseValuesAsync(cancellationToken)
            ?? throw new InvalidOperationException($"The entity with id {entity.Id} no longer exists in the database.");
        return ((TEntity)values.ToObject(), values);
    }

    /// <summary>
    /// Validates the entity's state by comparing with database values.
    /// Checks for soft deletion and concurrency conflicts.
    /// </summary>
    /// <param name="ignoreSoftDelete">When true, skips the soft delete check</param>
    protected virtual void ValidateEntityState(TEntity entity, TEntity databaseEntity, bool ignoreSoftDelete = false)
    {
        if (!ignoreSoftDelete && databaseEntity.DeletedAt.HasValue)
            throw new InvalidOperationException($"The entity with id {entity.Id} has been deleted by another user.");

        if (!databaseEntity.RowVersion.SequenceEqual(entity.RowVersion))
            throw new DbUpdateConcurrencyException(
                $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again."
            );
    }

    /// <summary>
    /// Standard messages for validation errors.
    /// </summary>
    protected struct Messages
    {
        public const string EntityCannotBeNull = "Entity cannot be null.";
        public const string CollectionCannotBeNull = "Collection cannot be null.";
        public const string CollectionContainsNullEntity = "Collection contains null entity.";
    }
}
