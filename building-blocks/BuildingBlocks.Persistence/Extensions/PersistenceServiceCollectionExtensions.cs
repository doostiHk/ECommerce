using BuildingBlocks.Common.Extensions;
using BuildingBlocks.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Persistence.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksPersistence(this IServiceCollection services)
    {
        services.AddBuildingBlocksCommon();
        services.TryAddScoped<AuditableEntitySaveChangesInterceptor>();
        return services;
    }

    public static DbContextOptionsBuilder UseBuildingBlocksInterceptors(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        optionsBuilder.AddInterceptors(
            serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        return optionsBuilder;
    }
}
