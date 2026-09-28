using BuildingBlocks.Authentication.Identity;
using BuildingBlocks.Common.Exceptions;
using FluentValidation;
using MediatR;
using Product.Api.Common;
using Product.Api.Common.Interfaces;
using Product.Api.Features.Products;

namespace Product.Api.Features.Products.Update;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    bool IsActive) : IRequest<ProductResponse>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    IKeycloakUserClient keycloakUserClient)
    : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.Id}' was not found.");

        if (await repository.SkuExistsAsync(request.Sku, request.Id, cancellationToken))
            throw new ConflictException("SKU already exists.");

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Sku = request.Sku.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;

        await repository.UpdateAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var users = await keycloakUserClient.GetDisplayNamesByIdsAsync(
            [product.CreatedByUserId, product.LastModifiedByUserId],
            cancellationToken);

        return product.ToResponse(users);
    }
}
