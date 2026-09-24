using BuildingBlocks.Authentication.Identity;
using BuildingBlocks.Common.Exceptions;
using FluentValidation;
using MediatR;
using Product.Api.Application.Common.Interfaces;
using Product.Api.Application.Features.Products.Mappings;
using Product.Api.Contracts;

namespace Product.Api.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse>;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetProductByIdQueryHandler(
    IProductRepository repository,
    IKeycloakUserClient keycloakUserClient)
    : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.Id}' was not found.");

        var users = await keycloakUserClient.GetDisplayNamesByIdsAsync(
            [product.CreatedByUserId, product.LastModifiedByUserId],
            cancellationToken);

        return product.ToResponse(users);
    }
}
