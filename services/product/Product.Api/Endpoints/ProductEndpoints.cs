using BuildingBlocks.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Api.Application.Features.Products.Commands.CreateProduct;
using Product.Api.Application.Features.Products.Commands.DeleteProduct;
using Product.Api.Application.Features.Products.Commands.UpdateProduct;
using Product.Api.Application.Features.Products.Queries.GetProductById;
using Product.Api.Application.Features.Products.Queries.GetProducts;
using Product.Api.Contracts;

namespace Product.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products");

        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .AllowAnonymous()
            .Produces<IReadOnlyList<ProductResponse>>();

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .AllowAnonymous()
            .Produces<ProductResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .RequireAuthorization(AuthConstants.AdminPolicy)
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .RequireAuthorization(AuthConstants.AdminPolicy)
            .Produces<ProductResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .RequireAuthorization(AuthConstants.AdminPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetProducts(ISender sender, CancellationToken cancellationToken)
    {
        var products = await sender.Send(new GetProductsQuery(), cancellationToken);
        return Results.Ok(products);
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Results.Ok(product);
    }

    private static async Task<IResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var product = await sender.Send(
            new CreateProductCommand(
                request.Name,
                request.Description,
                request.Sku,
                request.Price,
                request.StockQuantity,
                request.IsActive),
            cancellationToken);

        return Results.Created($"/products/{product.Id}", product);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var product = await sender.Send(
            new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Sku,
                request.Price,
                request.StockQuantity,
                request.IsActive),
            cancellationToken);

        return Results.Ok(product);
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
