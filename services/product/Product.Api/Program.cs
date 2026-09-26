using Product.Api.Features.Products;
using Product.Api.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProductInfrastructure(builder.Configuration);

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

await app.UseProductInfrastructureAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapProductEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
