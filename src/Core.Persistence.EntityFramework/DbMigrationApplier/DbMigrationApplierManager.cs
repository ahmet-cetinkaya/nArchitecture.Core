using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.EntityFramework.DbMigrationApplier;

/// <summary>
/// Implements a manager that applies database migrations using the provided DbContext.
/// </summary>
public class DbMigrationApplierManager<TDbContext>(TDbContext context) : IDbMigrationApplierService<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context = context;

    /// <inheritdoc />
    public Task Initialize()
    {
        // Apply database migrations based on database capabilities.
        _ = _context.Database.EnsureDbApplied();
        return Task.CompletedTask;
    }
}
