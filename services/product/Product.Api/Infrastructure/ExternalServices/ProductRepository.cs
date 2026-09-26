using Microsoft.EntityFrameworkCore;
using Product.Api.Common.Interfaces;
using Product.Api.Persistence;
using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Infrastructure.ExternalServices;

public sealed class ProductRepository(ProductDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ProductEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsQueryable();
        return excludeId is null
            ? query.AnyAsync(p => p.Sku == sku, cancellationToken)
            : query.AnyAsync(p => p.Sku == sku && p.Id != excludeId, cancellationToken);
    }

    public async Task AddAsync(ProductEntity product, CancellationToken cancellationToken = default) =>
        await db.Products.AddAsync(product, cancellationToken);

    public Task UpdateAsync(ProductEntity product, CancellationToken cancellationToken = default)
    {
        db.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ProductEntity product, CancellationToken cancellationToken = default)
    {
        db.Products.Remove(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
