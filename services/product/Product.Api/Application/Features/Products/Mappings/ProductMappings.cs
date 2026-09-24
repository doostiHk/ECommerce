using BuildingBlocks.Contracts.Users;
using Product.Api.Contracts;
using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Application.Features.Products.Mappings;

public static class ProductMappings
{
    public static ProductResponse ToResponse(
        this ProductEntity product,
        IReadOnlyDictionary<string, UserDisplayInfo>? users = null)
    {
        string? ResolveName(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || users is null)
                return null;

            return users.TryGetValue(userId, out var user) ? user.DisplayName : null;
        }

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.Price,
            product.StockQuantity,
            product.IsActive,
            product.CreatedAtUtc,
            product.CreatedByUserId,
            ResolveName(product.CreatedByUserId),
            product.LastModifiedAtUtc,
            product.LastModifiedByUserId,
            ResolveName(product.LastModifiedByUserId));
    }
}
