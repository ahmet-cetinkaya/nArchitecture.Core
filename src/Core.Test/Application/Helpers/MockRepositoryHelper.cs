using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using NArchitecture.Core.Persistence.Abstractions.Paging;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Test.Application.Helpers;

/// <summary>
/// Helper class for creating and configuring mock repositories for testing.
/// </summary>
public static class MockRepositoryHelper
{
    /// <summary>
    /// Creates a mock repository with configured basic CRUD operations.
    /// </summary>
    /// <typeparam name="TRepository">The type of repository to mock</typeparam>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TEntityId">The entity id type</typeparam>
    /// <param name="list">Initial data list for the repository</param>
    public static Mock<TRepository> GetRepository<TRepository, TEntity, TEntityId>(List<TEntity> list)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        var mockRepo = new Mock<TRepository>();

        Build<TRepository, TEntity, TEntityId>(mockRepo, list);
        return mockRepo;
    }

    // Private helper methods don't need XML documentation as they're implementation details
    private static void Build<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        SetupGetListAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupGetAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupAddAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupUpdateAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupDeleteAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupAnyAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupGetListByDynamicAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupGetRandomAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupGetRandomListAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupCountAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
        SetupCountLongAsync<TRepository, TEntity, TEntityId>(mockRepo, entityList);
    }

    private static void SetupGetListAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.GetListAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    int index,
                    int size,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IList<TEntity> list = new List<TEntity>();

                    if (!withDeleted)
                        list = entityList.Where(e => !e.DeletedAt.HasValue).ToList();
                    list = expression == null ? entityList : entityList.Where(expression.Compile()).ToList();

                    var paginatedList = list.Skip(index * size).Take(size).ToList();
                    Paginate<TEntity> paginateList = new()
                    {
                        Index = index,
                        Size = size,
                        Count = list.Count,
                        Pages = (int)Math.Ceiling(list.Count / (double)size),
                        Items = paginatedList,
                    };
                    return paginateList;
                }
            );
    }

    private static void SetupGetAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.GetAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    if (!withDeleted)
                        entityList = entityList.Where(e => !e.DeletedAt.HasValue).ToList();
                    TEntity? result = entityList.FirstOrDefault(predicate: expression.Compile());
                    return result;
                }
            );
    }

    private static void SetupAddAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(r => r.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (TEntity entity, CancellationToken cancellationToken) =>
                {
                    entityList.Add(entity);
                    return entity;
                }
            );
    }

    private static void SetupUpdateAsync<TRepository, TEntity, TEntityId2>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId2>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId2>, IRepository<TEntity, TEntityId2>
    {
        _ = mockRepo
            .Setup(r => r.UpdateAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))!
            .ReturnsAsync(
                (TEntity entity, CancellationToken cancellationToken) =>
                {
                    TEntity? result = entityList.FirstOrDefault(x => x.Id!.Equals(entity.Id));
                    if (result != null)
                        result = entity;
                    return result;
                }
            );
    }

    private static void SetupDeleteAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(r => r.DeleteAsync(It.IsAny<TEntity>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (TEntity entity, bool permanent, CancellationToken cancellationToken) =>
                {
                    if (!permanent)
                        entity.DeletedAt = DateTime.UtcNow;
                    else
                        _ = entityList.Remove(entity);
                    return entity;
                }
            );
    }

    public static void SetupAnyAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.AnyAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    if (!withDeleted)
                        entityList = entityList.Where(e => !e.DeletedAt.HasValue).ToList();
                    return entityList.Any(expression.Compile());
                }
            );
    }

    private static void SetupGetListByDynamicAsync<TRepository, TEntity, TEntityId>(
        Mock<TRepository> mockRepo,
        List<TEntity> entityList
    )
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.GetListByDynamicAsync(
                    It.IsAny<DynamicQuery>(),
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IQueryable<TEntity>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    DynamicQuery dynamic,
                    Expression<Func<TEntity, bool>>? expression,
                    Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
                    int index,
                    int size,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IList<TEntity> list = entityList;
                    if (!withDeleted)
                        list = list.Where(e => !e.DeletedAt.HasValue).ToList();
                    if (expression != null)
                        list = list.Where(expression.Compile()).ToList();

                    var paginatedList = list.Skip(index * size).Take(size).ToList();
                    return new Paginate<TEntity>
                    {
                        Index = index,
                        Size = size,
                        Count = list.Count,
                        Pages = (int)Math.Ceiling(list.Count / (double)size),
                        Items = paginatedList,
                    };
                }
            );
    }

    private static void SetupGetRandomAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.GetRandomAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>>? expression,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IList<TEntity> list = entityList;
                    if (!withDeleted)
                        list = list.Where(e => !e.DeletedAt.HasValue).ToList();
                    if (expression != null)
                        list = list.Where(expression.Compile()).ToList();

                    return list.Count > 0 ? list[new Random().Next(list.Count)] : null;
                }
            );
    }

    private static void SetupGetRandomListAsync<TRepository, TEntity, TEntityId>(
        Mock<TRepository> mockRepo,
        List<TEntity> entityList
    )
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.GetRandomListAsync(
                    It.IsAny<Expression<Func<TEntity, bool>>>(),
                    It.IsAny<Func<IQueryable<TEntity>, IQueryable<TEntity>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>>? expression,
                    Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
                    int index,
                    int size,
                    bool withDeleted,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IList<TEntity> list = entityList;
                    if (!withDeleted)
                        list = list.Where(e => !e.DeletedAt.HasValue).ToList();
                    if (expression != null)
                        list = list.Where(expression.Compile()).ToList();

                    var randomList = list.OrderBy(_ => Guid.NewGuid()).Take(size).ToList();
                    return new Paginate<TEntity>
                    {
                        Index = index,
                        Size = size,
                        Count = list.Count,
                        Pages = (int)Math.Ceiling(list.Count / (double)size),
                        Items = randomList,
                    };
                }
            );
    }

    private static void SetupCountAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.CountAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (Expression<Func<TEntity, bool>>? expression, bool withDeleted, CancellationToken cancellationToken) =>
                {
                    IList<TEntity> list = entityList;
                    if (!withDeleted)
                        list = list.Where(e => !e.DeletedAt.HasValue).ToList();
                    if (expression != null)
                        list = list.Where(expression.Compile()).ToList();

                    return list.Count;
                }
            );
    }

    private static void SetupCountLongAsync<TRepository, TEntity, TEntityId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : BaseEntity<TEntityId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    {
        _ = mockRepo
            .Setup(s =>
                s.CountLongAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (Expression<Func<TEntity, bool>>? expression, bool withDeleted, CancellationToken cancellationToken) =>
                {
                    IList<TEntity> list = entityList;
                    if (!withDeleted)
                        list = list.Where(e => !e.DeletedAt.HasValue).ToList();
                    if (expression != null)
                        list = list.Where(expression.Compile()).ToList();

                    return list.LongCount();
                }
            );
    }
}
