using System.Collections;
using System.Reflection;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Repositories;

public partial class EfRepositoryBase<TEntity, TEntityId, TContext>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    // Public Methods
    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        SetEntityAsDeleted(entity, permanent, isAsync: false).Wait();
        return entity;
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default)
    {
        await SetEntityAsDeleted(entity, permanent, isAsync: true, cancellationToken);
        return entity;
    }

    public void BulkDelete(ICollection<TEntity> entities, bool permanent = false, int batchSize = 1_000)
    {
        if (entities.Count == 0)
            return;

        if (permanent)
        {
            foreach (var batch in entities.Chunk(batchSize))
            {
                Context.BulkDelete(batch);
            }
        }
        else
        {
            foreach (var batch in entities.Chunk(batchSize))
            {
                SetEntityAsDeleted(batch, permanent, isAsync: false).Wait();
                Context.BulkUpdate(batch);
            }
        }
    }

    public async Task BulkDeleteAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default
    )
    {
        if (entities.Count == 0)
            return;

        if (permanent)
            foreach (var batch in entities.Chunk(batchSize))
            {
                await Context.BulkDeleteAsync(batch, cancellationToken: cancellationToken);
            }
        else
            foreach (var batch in entities.Chunk(batchSize))
            {
                await SetEntityAsDeleted(batch, permanent, isAsync: true, cancellationToken);
                await Context.BulkUpdateAsync(batch, cancellationToken: cancellationToken);
            }
    }

    // Protected Methods
    protected async Task SetEntityAsDeleted(
        TEntity entity,
        bool permanent,
        bool isAsync = true,
        CancellationToken cancellationToken = default
    )
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            if (isAsync)
                await setEntityAsSoftDeleted(entity, isAsync, cancellationToken: cancellationToken);
            else
                setEntityAsSoftDeleted(entity, isAsync, cancellationToken: cancellationToken).Wait(cancellationToken);
        }
        else
            _ = Context.Remove(entity);
    }

    protected async Task SetEntityAsDeleted(
        IEnumerable<TEntity> entities,
        bool permanent,
        bool isAsync = true,
        CancellationToken cancellationToken = default
    )
    {
        foreach (TEntity entity in entities)
            await SetEntityAsDeleted(entity, permanent, isAsync, cancellationToken);
    }

    private const string CreateQueryNotFound = "CreateQuery<TElement> method is not found in IQueryProvider.";

    protected IQueryable<object>? GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();
        MethodInfo createQueryMethod =
            queryProviderType
                .GetMethods()
                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
                ?.MakeGenericMethod(navigationPropertyType) ?? throw new InvalidOperationException(CreateQueryNotFound);
        var queryProviderQuery = (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: [query.Expression])!;
        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }

    private const string EntityHasOneToOneRelation =
        "Entity {0} has a one-to-one relationship with {1} via the primary key ({2}). "
        + "Soft Delete causes problems if you try to create an entry again with the same foreign key.";

    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        IEnumerable<IForeignKey> foreignKeys = Context.Entry(entity).Metadata.GetForeignKeys();
        IForeignKey? oneToOneForeignKey = foreignKeys.FirstOrDefault(fk =>
            fk.IsUnique && fk.PrincipalKey.Properties.All(pk => Context.Entry(entity).Property(pk.Name).Metadata.IsPrimaryKey())
        );

        if (oneToOneForeignKey != null)
        {
            string relatedEntity = oneToOneForeignKey.PrincipalEntityType.ClrType.Name;
            IReadOnlyList<IProperty> primaryKeyProperties = Context.Entry(entity).Metadata.FindPrimaryKey()!.Properties;
            string primaryKeyNames = string.Join(", ", primaryKeyProperties.Select(prop => prop.Name));
            throw new InvalidOperationException(
                string.Format(EntityHasOneToOneRelation, entity.GetType().Name, relatedEntity, primaryKeyNames)
            );
        }
    }

    protected virtual void EditEntityPropertiesToDelete(TEntity entity)
    {
        entity.DeletedDate = DateTime.UtcNow;
    }

    protected virtual void EditRelationEntityPropertiesToCascadeSoftDelete(IEntityTimestamps entity)
    {
        entity.DeletedDate = DateTime.UtcNow;
    }

    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedDate.HasValue;
    }

    // Private Methods
    private async Task setEntityAsSoftDeleted(
        IEntityTimestamps entity,
        bool isAsync = true,
        bool isRoot = true,
        CancellationToken cancellationToken = default
    )
    {
        if (IsSoftDeleted(entity))
            return;

        EditRelationEntityPropertiesToCascadeSoftDelete(entity);
        if (isRoot)
            EditEntityPropertiesToDelete((TEntity)entity);
        else
            EditRelationEntityPropertiesToCascadeSoftDelete(entity);

        var navigations = Context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x =>
                x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade }
            )
            .ToList();
        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();

                    if (isAsync)
                    {
                        IQueryable<object>? relationLoaderQuery = GetRelationLoaderQuery(
                            query,
                            navigationPropertyType: navigation.PropertyInfo.GetType()
                        );
                        if (relationLoaderQuery is not null)
                            navValue = await relationLoaderQuery.ToListAsync(cancellationToken);
                    }
                    else
                        navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                            ?.ToList();

                    if (navValue == null)
                        continue;
                }

                foreach (object navValueItem in (IEnumerable)navValue)
                    await setEntityAsSoftDeleted((IEntityTimestamps)navValueItem, isAsync, isRoot: false, cancellationToken);
            }
            else
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();

                    if (isAsync)
                    {
                        IQueryable<object>? relationLoaderQuery = GetRelationLoaderQuery(
                            query,
                            navigationPropertyType: navigation.PropertyInfo.GetType()
                        );
                        if (relationLoaderQuery is not null)
                            navValue = await relationLoaderQuery.FirstOrDefaultAsync(cancellationToken);
                    }
                    else
                        navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                            ?.FirstOrDefault();

                    if (navValue == null)
                        continue;
                }

                await setEntityAsSoftDeleted((IEntityTimestamps)navValue, isAsync, isRoot: false, cancellationToken);
            }
        }

        _ = Context.Update(entity);
    }
}
