using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.EntityFramework.DbMigrationApplier;

public class DbMigrationApplierManager<TDbContext>(TDbContext context) : IDbMigrationApplierService<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context = context;

    public Task Initialize()
    {
        _ = _context.Database.EnsureDbApplied();
        return Task.CompletedTask;
    }
}
