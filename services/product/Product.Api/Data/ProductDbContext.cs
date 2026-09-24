using BuildingBlocks.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : BaseDbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var product = modelBuilder.Entity<ProductEntity>();

        product.ToTable("products");
        product.HasKey(p => p.Id);

        product.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        product.Property(p => p.Description)
            .HasMaxLength(2000);

        product.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(64);

        product.HasIndex(p => p.Sku)
            .IsUnique();

        product.Property(p => p.Price)
            .HasPrecision(18, 2);
    }
}
