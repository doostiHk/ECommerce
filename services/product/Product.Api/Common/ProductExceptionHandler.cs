using BuildingBlocks.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Product.Api.Common;

public sealed class ProductExceptionHandler(ILogger<ProductExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Errors),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                exception.Message,
                (IDictionary<string, string[]>?)null),
            ConflictException => (
                StatusCodes.Status409Conflict,
                exception.Message,
                null),
            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                exception.Message,
                null),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                exception.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = statusCode;

        if (errors is not null)
        {
            await httpContext.Response.WriteAsJsonAsync(
                new HttpValidationProblemDetails(errors)
                {
                    Status = statusCode,
                    Title = title
                },
                cancellationToken);
            return true;
        }

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title
            },
            cancellationToken);

        return true;
    }
}
