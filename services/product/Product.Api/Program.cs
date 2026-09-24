using System.Reflection;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.Persistence.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Api.Application.Common.Behaviors;
using Product.Api.Application.Common.Interfaces;
using Product.Api.Data;
using Product.Api.Endpoints;
using Product.Api.Infrastructure;
using Product.Api.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationSwagger("Product API", "v1", "Product catalog CRUD service");
builder.Services.AddBuildingBlocksPersistence();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ProductDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    options.UseBuildingBlocksInterceptors(sp);
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<ProductExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapProductEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
