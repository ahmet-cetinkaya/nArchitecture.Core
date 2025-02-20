using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.EntityFramework.DependencyInjection;

/// <summary>
/// Provides extension methods to apply database migrations for any .NET application.
/// </summary>
public static class ServiceProviderDbMigrationApplierExtensions
{
    /// <summary>
    /// Applies database migrations by initializing all registered <see cref="IDbMigrationApplierService"/> services.
    /// </summary>
    /// <param name="serviceProvider">The service provider instance.</param>
    /// <returns>The same service provider instance to allow method chaining.</returns>
    public static IServiceProvider UseDbMigrationApplier(this IServiceProvider serviceProvider)
    {
        // Retrieve all registered migration applier services.
        IEnumerable<IDbMigrationApplierService> migrationApplierServices =
            serviceProvider.GetServices<IDbMigrationApplierService>();

        // Initialize each migration applier service.
        foreach (IDbMigrationApplierService service in migrationApplierServices)
            _ = service.Initialize();

        return serviceProvider;
    }
}
