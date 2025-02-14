namespace NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

/// <summary>
/// Defines a service for applying database migrations.
/// </summary>
public interface IDbMigrationApplierService
{
    /// <summary>
    /// Initializes the database migration process.
    /// </summary>
    Task Initialize();
}

/// <summary>
/// Defines a service for applying database migrations for a specific DbContext.
/// </summary>
/// <typeparam name="TDbContext">The type representing the database context.</typeparam>
public interface IDbMigrationApplierService<TDbContext> : IDbMigrationApplierService
    where TDbContext : class { }
