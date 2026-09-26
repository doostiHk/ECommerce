namespace Product.Api.Common;

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedByUserId,
    string? CreatedByUserName,
    DateTime? LastModifiedAtUtc,
    string? LastModifiedByUserId,
    string? LastModifiedByUserName);

public record CreateProductRequest(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive = true);

public record UpdateProductRequest(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive);
