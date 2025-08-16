using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NArchitecture.Core.Persistence.EntityFramework.DbMigrationApplier;

/// <summary>
/// Provides extension methods for the DatabaseFacade to apply migrations.
/// </summary>
public static class DatabaseFacadeDbMigrationApplierExtensions
{
    /// <summary>
    /// Ensures that the database is applied by creating, migrating, or connecting as needed.
    /// </summary>
    /// <param name="databaseFacade">The DatabaseFacade instance.</param>
    /// <returns>The updated DatabaseFacade.</returns>
    public static DatabaseFacade EnsureDbApplied(this DatabaseFacade databaseFacade)
    {
        // Check if a connection can be made.
        if (!databaseFacade.CanConnect())
            return databaseFacade;

        // For in-memory databases, ensure creation.
        if (databaseFacade.IsInMemory())
            _ = databaseFacade.EnsureCreated();

        // For relational databases, apply migrations.
        if (databaseFacade.IsRelational())
            databaseFacade.Migrate();

        return databaseFacade;
    }
}
