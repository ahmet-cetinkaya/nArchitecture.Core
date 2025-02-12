namespace NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

public interface IDbMigrationApplierService
{
    public Task Initialize();
}

public interface IDbMigrationApplierService<TDbContext> : IDbMigrationApplierService
    where TDbContext : class { }
