using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NArchitecture.Core.Persistence.Abstractions.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Extensions;

/// <summary>
/// Provides extension methods for Entity Framework model configuration.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures common entity properties and behaviors including soft delete and auditing.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder ConfigureBaseEntities<TId>(this ModelBuilder modelBuilder)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<TId>).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            // Configure RowVersion for optimistic concurrency
            _ = modelBuilder.Entity(entityType.ClrType).Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();

            // Configure soft delete filter
            _ = modelBuilder.Entity(entityType.ClrType).HasQueryFilter(CreateSoftDeleteFilterExpression(entityType.ClrType));
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures common entity properties and behaviors for all entities that inherit from BaseEntity.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder UseOptimisticConcurrency<TId>(this ModelBuilder modelBuilder)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<TId>).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            // Configure RowVersion for optimistic concurrency
            _ = modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(BaseEntity<TId>.RowVersion))
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnType("rowversion");

            // Configure UpdatedAt as additional concurrency token
            _ = modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity<TId>.UpdatedAt)).IsConcurrencyToken();
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures optimistic concurrency for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder instance.</param>
    /// <returns>The configured entity type builder instance.</returns>
    public static EntityTypeBuilder<TEntity> UseOptimisticConcurrency<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        _ = builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();

        return builder;
    }

    /// <summary>
    /// Configures global query filters for soft delete functionality.
    /// </summary>
    /// <typeparam name="TId">The type of entity identifier.</typeparam>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder UseSoftDelete<TId>(this ModelBuilder modelBuilder)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<TId>).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            // Configure soft delete filter for each entity
            _ = modelBuilder.Entity(entityType.ClrType).HasQueryFilter(CreateSoftDeleteFilterExpression(entityType.ClrType));
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures soft delete functionality for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder instance.</param>
    /// <returns>The configured entity type builder instance.</returns>
    public static EntityTypeBuilder<TEntity> UseSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntityTimestamps
    {
        var expression = (Expression<Func<TEntity, bool>>)CreateSoftDeleteFilterExpression(typeof(TEntity));
        _ = builder.HasQueryFilter(expression);
        return builder;
    }

    /// <summary>
    /// Creates a soft delete query filter expression for the given entity type.
    /// </summary>
    private static LambdaExpression CreateSoftDeleteFilterExpression(Type entityType)
    {
        ParameterExpression parameter = Expression.Parameter(entityType, "e");
        System.Reflection.MethodInfo? propertyMethodInfo = typeof(EF).GetMethod("Property")?.MakeGenericMethod(typeof(DateTime?));
        MethodCallExpression deletedAtProperty = Expression.Call(
            propertyMethodInfo!,
            parameter,
            Expression.Constant("DeletedAt")
        );
        ConstantExpression nullConstant = Expression.Constant(null, typeof(DateTime?));
        BinaryExpression compareExpression = Expression.Equal(deletedAtProperty, nullConstant);
        return Expression.Lambda(compareExpression, parameter);
    }
}
