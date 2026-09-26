using BuildingBlocks.Authentication.Identity;
using MediatR;
using Product.Api.Common;
using Product.Api.Common.Interfaces;
using Product.Api.Features.Products;

namespace Product.Api.Features.Products.GetList;

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
