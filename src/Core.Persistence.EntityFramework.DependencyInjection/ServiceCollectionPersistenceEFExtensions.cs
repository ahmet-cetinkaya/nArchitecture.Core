using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;
using NArchitecture.Core.Persistence.EntityFramework.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.DependencyInjection;

public static class ServiceCollectionPersistenceEFExtensions
{
    public static IServiceCollection AddDbMigrationApplier<TDbContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TDbContext> contextFactory
    )
        where TDbContext : DbContext
    {
        ServiceProvider buildServiceProvider = services.BuildServiceProvider();

        _ = services.AddTransient<IDbMigrationApplierService, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );
        _ = services.AddTransient<IDbMigrationApplierService<TDbContext>, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );

        return services;
    }
}
