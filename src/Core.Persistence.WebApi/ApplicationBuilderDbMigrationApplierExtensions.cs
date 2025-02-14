using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.Abstractions.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.WebApi;

public static class ApplicationBuilderDbMigrationApplierExtensions
{
    public static IApplicationBuilder UseDbMigrationApplier(this IApplicationBuilder app)
    {
        IEnumerable<IDbMigrationApplierService> migrationCreatorServices =
            app.ApplicationServices.GetServices<IDbMigrationApplierService>();
        foreach (IDbMigrationApplierService service in migrationCreatorServices)
            _ = service.Initialize();

        return app;
    }
}
