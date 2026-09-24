using BuildingBlocks.Authentication.Identity;
using MediatR;
using Product.Api.Application.Common.Interfaces;
using Product.Api.Application.Features.Products.Mappings;
using Product.Api.Contracts;

namespace Product.Api.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery : IRequest<IReadOnlyList<ProductResponse>>;

public sealed class GetProductsQueryHandler(
    IProductRepository repository,
    IKeycloakUserClient keycloakUserClient)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<IReadOnlyList<ProductResponse>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);

        var userIds = products
            .SelectMany(p => new[] { p.CreatedByUserId, p.LastModifiedByUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>();

        var users = await keycloakUserClient.GetDisplayNamesByIdsAsync(userIds, cancellationToken);
        return products.Select(p => p.ToResponse(users)).ToList();
    }
}
