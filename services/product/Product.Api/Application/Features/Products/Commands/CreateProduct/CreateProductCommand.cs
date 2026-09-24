using BuildingBlocks.Authentication.Identity;
using BuildingBlocks.Common.Exceptions;
using FluentValidation;
using MediatR;
using Product.Api.Application.Common.Interfaces;
using Product.Api.Application.Features.Products.Mappings;
using Product.Api.Contracts;
using ProductEntity = Product.Api.Domain.Entities.Product;

namespace Product.Api.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive) : IRequest<ProductResponse>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IKeycloakUserClient keycloakUserClient)
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await repository.SkuExistsAsync(request.Sku, cancellationToken: cancellationToken))
            throw new ConflictException("SKU already exists.");

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Sku = request.Sku.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive
        };

        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var users = await keycloakUserClient.GetDisplayNamesByIdsAsync(
            [product.CreatedByUserId, product.LastModifiedByUserId],
            cancellationToken);

        return product.ToResponse(users);
    }
}
