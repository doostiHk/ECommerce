using System.Security.Claims;
using BuildingBlocks.Authentication.Extensions;
using BuildingBlocks.Authentication.Identity;
using BuildingBlocks.Authentication.Options;
using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace BuildingBlocks.Authentication.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksCommon();

        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.Configure<KeycloakAdminOptions>(configuration.GetSection(KeycloakAdminOptions.SectionName));

        var keycloak = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
            ?? throw new InvalidOperationException($"{KeycloakOptions.SectionName} is not configured.");

        if (string.IsNullOrWhiteSpace(keycloak.Authority))
            throw new InvalidOperationException("Keycloak:Authority is not configured.");

        var validIssuers = keycloak.ValidIssuers is { Length: > 0 }
            ? keycloak.ValidIssuers
            : [keycloak.Authority];

        var metadataAddress = string.IsNullOrWhiteSpace(keycloak.MetadataAddress)
            ? $"{keycloak.Authority.TrimEnd('/')}/.well-known/openid-configuration"
            : keycloak.MetadataAddress;

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, CurrentUser>();

        services.AddMemoryCache();
        services.AddHttpClient("keycloak-admin");
        services.TryAddScoped<IKeycloakUserClient, KeycloakUserClient>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloak.Authority;
                options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = validIssuers,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is ClaimsIdentity identity)
                            identity.MapKeycloakRoles();
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthConstants.AuthenticatedPolicy, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthConstants.AdminPolicy, policy =>
                policy.RequireRole(string.IsNullOrWhiteSpace(keycloak.AdminRole)
                    ? AuthConstants.AdminRole
                    : keycloak.AdminRole));
        });

        return services;
    }

    public static IServiceCollection AddApplicationSwagger(
        this IServiceCollection services,
        string title,
        string version = "v1",
        string? description = null)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste a Keycloak access token."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }
}
