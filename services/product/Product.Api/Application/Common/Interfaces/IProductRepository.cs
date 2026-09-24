using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task DeleteAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
