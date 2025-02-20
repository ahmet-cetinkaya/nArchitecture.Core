using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;
using NArchitecture.Core.Persistence.EntityFramework.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.EntityFramework.DependencyInjection;

public static class ServiceCollectionPersistenceEFExtensions
{
    /// <summary>
    /// Registers the DB migration applier services for the specified DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="contextFactory">A factory function to create instances of the DbContext.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDbMigrationApplier<TDbContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TDbContext> contextFactory
    )
        where TDbContext : DbContext
    {
        // Build a service provider to resolve dependencies.
        ServiceProvider buildServiceProvider = services.BuildServiceProvider();

        // Register non-generic migration applier service.
        _ = services.AddTransient<IDbMigrationApplierService, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );
        // Register generic migration applier service.
        _ = services.AddTransient<IDbMigrationApplierService<TDbContext>, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );

        return services;
    }
}
