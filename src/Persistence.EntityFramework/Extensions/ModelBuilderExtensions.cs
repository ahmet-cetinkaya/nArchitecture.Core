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
    /// <param name="createdAtColumnName">The name of the created at column.</param>
    /// <param name="updatedAtColumnName">The name of the updated at column.</param>
    /// <param name="deletedAtColumnName">The name of the deleted at column.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder ApplyTimestampsConfiguration(
        this ModelBuilder modelBuilder,
        string createdAtColumnName = "CreatedAt",
        string updatedAtColumnName = "UpdatedAt",
        string deletedAtColumnName = "DeletedAt"
    )
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<>).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            // Configure timestamps properties
            _ = modelBuilder
                .Entity(entityType.ClrType)
                .Property<DateTime>("CreatedAt")
                .IsRequired()
                .ValueGeneratedOnAdd()
                .HasColumnName(createdAtColumnName);
            _ = modelBuilder
                .Entity(entityType.ClrType)
                .Property<DateTime?>("UpdatedAt")
                .ValueGeneratedOnUpdate()
                .HasColumnName(updatedAtColumnName);
            _ = modelBuilder.Entity(entityType.ClrType).Property<DateTime?>("DeletedAt").HasColumnName(deletedAtColumnName);
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures common entity properties and behaviors for all entities that inherit from BaseEntity.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder ApplyOptimisticConcurrencyConfiguration(this ModelBuilder modelBuilder)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<>).IsAssignableFrom(e.ClrType));

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType? entityType in entityTypes)
        {
            // Configure RowVersion for optimistic concurrency
            _ = modelBuilder
                .Entity(entityType.ClrType)
                .Property("RowVersion")
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnType("rowversion");
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures optimistic concurrency for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder instance.</param>
    /// <returns>The configured entity type builder instance.</returns>
    public static EntityTypeBuilder<TEntity> ApplyOptimisticConcurrencyConfiguration<TEntity>(
        this EntityTypeBuilder<TEntity> builder
    )
        where TEntity : class
    {
        _ = builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();

        return builder;
    }

    /// <summary>
    /// Configures global query filters for soft delete functionality.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <returns>The configured model builder instance.</returns>
    public static ModelBuilder ApplySoftDeleteConfiguration(this ModelBuilder modelBuilder)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity<>).IsAssignableFrom(e.ClrType));

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
    public static EntityTypeBuilder<TEntity> ApplySoftDeleteConfiguration<TEntity>(this EntityTypeBuilder<TEntity> builder)
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
