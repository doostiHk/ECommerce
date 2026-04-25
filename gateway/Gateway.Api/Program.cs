using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------

// -------------------------------------------------------------------
var keycloakUrl = builder.Configuration["Keycloak:Url"];           
var realm = builder.Configuration["Keycloak:Realm"];         
var apiClientId = builder.Configuration["Keycloak:ApiClientId"];  
var authority = $"{keycloakUrl}/realms/{realm}";
var audience = apiClientId;

// -------------------------------------------------------------------
// Authentication
// -------------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "preferred_username",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var identity = (ClaimsIdentity)ctx.Principal.Identity;

                var realmAccess = ctx.Principal.FindFirst("realm_access");
                if (realmAccess != null)
                {
                    using var doc = JsonDocument.Parse(realmAccess.Value);
                    foreach (var role in doc.RootElement.GetProperty("roles").EnumerateArray())
                    {
                        identity.AddClaim(new Claim("role", role.GetString()));
                    }
                }

                var resourceAccess = ctx.Principal.FindFirst("resource_access");
                if (resourceAccess != null)
                {
                    using var doc = JsonDocument.Parse(resourceAccess.Value);

                    if (doc.RootElement.TryGetProperty("realm-manager", out var client))
                    {
                        foreach (var role in client.GetProperty("roles").EnumerateArray())
                        {
                            identity.AddClaim(new Claim("role", role.GetString()));
                        }
                    }
                }

                ctx.Principal = new ClaimsPrincipal(identity);
                await Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireProductRole",
        policy => policy.RequireRole("product:read"));
    options.AddPolicy("RequireOrderRole",
        policy => policy.RequireRole("order:read"));
});

// -------------------------------------------------------------------
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("Yarp:ReverseProxy"))
    .AddTransforms(transforms =>
        transforms.AddRequestTransform(async ctx =>
        {
            await ValueTask.CompletedTask;
        }));

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapReverseProxy();            

app.Run();
