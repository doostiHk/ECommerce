using BuildingBlocks.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Persistence;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : BaseDbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
    }
}
