using BuildingBlocks.Authentication.Extensions;

namespace Gateway.Api.Auth;

public static class GatewaySwaggerExtensions
{
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services) =>
        services.AddApplicationSwagger(
            "API Gateway",
            "v1",
            "YARP gateway with Keycloak authentication");
}
