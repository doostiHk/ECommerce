using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Common.Extensions;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocksCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }
}
