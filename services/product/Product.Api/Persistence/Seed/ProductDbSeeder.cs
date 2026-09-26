namespace Product.Api.Persistence.Seed;

public static class ProductDbSeeder
{
    public static Task SeedAsync(ProductDbContext dbContext, CancellationToken cancellationToken = default)
    {
        _ = dbContext;
        return Task.CompletedTask;
    }
}
