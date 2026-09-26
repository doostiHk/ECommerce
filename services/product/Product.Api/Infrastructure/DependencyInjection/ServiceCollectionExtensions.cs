using System.Reflection;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.Persistence.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Api.Common;
using Product.Api.Common.Behaviors;
using Product.Api.Common.Interfaces;
using Product.Api.Infrastructure.ExternalServices;
using Product.Api.Persistence;
using Product.Api.Persistence.Seed;

namespace Product.Api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationAuthentication(configuration);
        services.AddApplicationSwagger("Product API", "v1", "Product catalog CRUD service");
        services.AddBuildingBlocksPersistence();
        services.AddHealthChecks();

        services.AddDbContext<ProductDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
            options.UseBuildingBlocksInterceptors(sp);
        });

        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddExceptionHandler<ProductExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static async Task UseProductInfrastructureAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.MigrateAsync();
        await ProductDbSeeder.SeedAsync(db);
    }
}
