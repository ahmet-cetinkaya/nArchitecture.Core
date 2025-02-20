using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Persistence.EntityFramework.Dynamic;
using NArchitecture.Core.Persistence.EntityFramework.Paging;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : BaseEntity<TEntityId>
    where TContext : DbContext
{
    /// <inheritdoc/>
    public TEntity? Get(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return queryable.FirstOrDefault(predicate);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc/>
    public TEntity? GetById(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return queryable.FirstOrDefault(e => e.Id!.Equals(id));
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetByIdAsync(
        TEntityId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);
    }

    /// <inheritdoc/>
    public ICollection<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int fetchLimit = 1_000_000,
        int chunkSize = 1_000
    )
    {
        if (chunkSize <= 0)
            throw new ArgumentException(ReadMessages.ChunkSizeMustBeGreaterThanZero, nameof(chunkSize));

        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);

        int totalCount = queryable.Count();
        if (totalCount > fetchLimit)
            throw new InvalidOperationException(string.Format(ReadMessages.ResultSetTooLarge, fetchLimit, totalCount));

        var results = new List<TEntity>();
        int processedCount = 0;

        while (processedCount < totalCount)
        {
            var chunk = queryable.Skip(processedCount).Take(chunkSize).ToList();
            if (chunk.Count == 0)
                break;

            results.AddRange(chunk);
            processedCount += chunk.Count;
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        int fetchLimit = 1_000_000,
        int chunkSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (chunkSize <= 0)
            throw new ArgumentException(ReadMessages.ChunkSizeMustBeGreaterThanZero, nameof(chunkSize));

        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);

        int totalCount = await queryable.CountAsync(cancellationToken);
        if (totalCount > fetchLimit)
            throw new InvalidOperationException(string.Format(ReadMessages.ResultSetTooLarge, fetchLimit, totalCount));

        var results = new HashSet<TEntity>();
        int processedCount = 0;

        while (processedCount < totalCount)
        {
            List<TEntity> chunk = await queryable.Skip(processedCount).Take(chunkSize).ToListAsync(cancellationToken);
            if (chunk.Count == 0)
                break;

            results.UnionWith(chunk);
            processedCount += chunk.Count;

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        return results;
    }

    private const int MaxPageSize = 100_000;

    private static void validatePaginationParameters(int index, int size)
    {
        if (index < 0)
            throw new ArgumentException(ReadMessages.PageIndexNegativeMsg, nameof(index));
        if (index == int.MaxValue)
            throw new ArgumentException(ReadMessages.PageIndexTooLarge, nameof(index));

        if (size <= 0)
            throw new ArgumentException(ReadMessages.PageSizeMustBeGreaterThanZero, nameof(size));
        if (size == int.MaxValue || size > MaxPageSize)
            throw new ArgumentException(ReadMessages.PageSizeTooLarge, nameof(size));

        if (index >= int.MaxValue / size)
            throw new ArgumentException(ReadMessages.PageIndexSizeCombinationOverflow, nameof(index));
    }

    /// <inheritdoc/>
    public IPaginate<TEntity> GetList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        EfRepositoryBase<TEntity, TEntityId, TContext>.validatePaginationParameters(index, size);
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return orderBy(queryable).ToPaginate(index, size);
        return queryable.ToPaginate(index, size);
    }

    /// <inheritdoc/>
    public async Task<IPaginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        EfRepositoryBase<TEntity, TEntityId, TContext>.validatePaginationParameters(index, size);
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return await orderBy(queryable).ToPaginateAsync(index, size, cancellationToken);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    /// <inheritdoc/>
    public IPaginate<TEntity> GetListByDynamic(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        EfRepositoryBase<TEntity, TEntityId, TContext>.validatePaginationParameters(index, size);
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.ToPaginate(index, size);
    }

    /// <inheritdoc/>
    public async Task<IPaginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        EfRepositoryBase<TEntity, TEntityId, TContext>.validatePaginationParameters(index, size);
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    /// <inheritdoc/>
    public bool ExistsById(TEntityId id, bool withDeleted = false)
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return queryable.Any(x => x.Id!.Equals(id));
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIdAsync(TEntityId id, bool withDeleted = false, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.AnyAsync(x => x.Id!.Equals(id), cancellationToken);
    }

    /// <inheritdoc/>
    public TEntity? GetRandom(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.OrderBy(x => EF.Functions.Random()).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetRandomAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.OrderBy(x => EF.Functions.Random()).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IPaginate<TEntity> GetRandomList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.OrderBy(x => EF.Functions.Random()).ToPaginate(index, size);
    }

    /// <inheritdoc/>
    public async Task<IPaginate<TEntity>> GetRandomListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.OrderBy(x => EF.Functions.Random()).ToPaginateAsync(index, size, cancellationToken);
    }

    /// <inheritdoc/>
    public int Count(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false)
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.Count();
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public long CountLong(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false)
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.LongCount();
    }

    /// <inheritdoc/>
    public async Task<long> CountLongAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public bool Any(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.Any();
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.AnyAsync(cancellationToken);
    }
}

file static class ReadMessages
{
    public const string ChunkSizeMustBeGreaterThanZero = "Chunk size must be greater than 0.";
    public const string PageIndexNegativeMsg = "Page index cannot be negative.";
    public const string PageIndexTooLarge = "Page index is too large.";
    public const string PageSizeMustBeGreaterThanZero = "Page size must be greater than 0.";
    public const string PageSizeTooLarge = "Page size is too large.";
    public const string PageIndexSizeCombinationOverflow = "Page index and size combination would cause arithmetic overflow.";
    public const string ResultSetTooLarge = "Result set too large. Maximum allowed: {0}, Actual: {1}";
}
