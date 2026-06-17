using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var authority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
var clientId = builder.Configuration["Keycloak:ClientId"]
    ?? throw new InvalidOperationException("Keycloak:ClientId is not configured.");

// ── Authentication ────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidIssuer      = authority,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.FromSeconds(30),
            NameClaimType    = "preferred_username",
            RoleClaimType    = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var identity = (ClaimsIdentity)ctx.Principal!.Identity!;

                // realm-level roles → claim "role"
                var realmAccess = ctx.Principal.FindFirst("realm_access");
                if (realmAccess is not null)
                {
                    using var doc = JsonDocument.Parse(realmAccess.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                        foreach (var r in roles.EnumerateArray())
                            identity.AddClaim(new Claim("role", r.GetString()!));
                }

                // client-level roles → claim "role" (format: clientId:roleName)
                var resourceAccess = ctx.Principal.FindFirst("resource_access");
                if (resourceAccess is not null)
                {
                    using var doc = JsonDocument.Parse(resourceAccess.Value);
                    foreach (var client in doc.RootElement.EnumerateObject())
                        if (client.Value.TryGetProperty("roles", out var roles))
                            foreach (var r in roles.EnumerateArray())
                                identity.AddClaim(new Claim("role", $"{client.Name}:{r.GetString()}"));
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── HttpClient for Keycloak token endpoint ────────────────────────────
builder.Services.AddHttpClient("keycloak");

// ── YARP Reverse Proxy ────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Auth endpoints ────────────────────────────────────────────────────
var auth = app.MapGroup("/auth");

// POST /auth/login  →  username + password  →  tokens
auth.MapPost("/login", async (
    LoginRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url  = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/token";
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"]  = cfg["Keycloak:ClientId"]!,
        ["username"]   = req.Username,
        ["password"]   = req.Password,
        ["scope"]      = "openid profile email"
    };

    var res = await factory.CreateClient("keycloak")
                           .PostAsync(url, new FormUrlEncodedContent(form));

    if (!res.IsSuccessStatusCode)
        return Results.Problem(title: "Invalid credentials", statusCode: 401);

    var token = await res.Content.ReadFromJsonAsync<TokenResponse>();
    return Results.Ok(token);
}).AllowAnonymous();

// POST /auth/refresh  →  refresh_token  →  new tokens
auth.MapPost("/refresh", async (
    RefreshRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url  = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/token";
    var form = new Dictionary<string, string>
    {
        ["grant_type"]    = "refresh_token",
        ["client_id"]     = cfg["Keycloak:ClientId"]!,
        ["refresh_token"] = req.RefreshToken
    };

    var res = await factory.CreateClient("keycloak")
                           .PostAsync(url, new FormUrlEncodedContent(form));

    if (!res.IsSuccessStatusCode)
        return Results.Problem(title: "Invalid or expired refresh token", statusCode: 401);

    var token = await res.Content.ReadFromJsonAsync<TokenResponse>();
    return Results.Ok(token);
}).AllowAnonymous();

// POST /auth/register  →  creates a new user via Keycloak Admin API
auth.MapPost("/register", async (
    RegisterRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var authority = cfg["Keycloak:Authority"]!;
    // parse "http://keycloak:8080/realms/Core"  →  baseUrl + realm
    var parts   = authority.Split("/realms/", 2);
    var baseUrl = parts[0];
    var realm   = parts[1];
    var http    = factory.CreateClient("keycloak");

    // 1. get a short-lived admin token from the master realm
    var tokenUrl  = $"{baseUrl}/realms/master/protocol/openid-connect/token";
    var tokenForm = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"]  = "admin-cli",
        ["username"]   = cfg["Keycloak:AdminUsername"]!,
        ["password"]   = cfg["Keycloak:AdminPassword"]!
    };

    var tokenRes = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenForm));
    if (!tokenRes.IsSuccessStatusCode)
        return Results.Problem(title: "Authentication service unavailable", statusCode: 503);

    var tokenDoc    = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
    var adminToken  = tokenDoc.GetProperty("access_token").GetString()!;

    // 2. create the user via Admin REST API
    var createUrl = $"{baseUrl}/admin/realms/{realm}/users";
    var userBody  = new
    {
        username    = req.Username,
        email       = req.Email,
        firstName   = req.FirstName,
        lastName    = req.LastName,
        enabled     = true,
        credentials = new[]
        {
            new { type = "password", value = req.Password, temporary = false }
        }
    };

    using var createReq = new HttpRequestMessage(HttpMethod.Post, createUrl);
    createReq.Headers.Authorization = new("Bearer", adminToken);
    createReq.Content = JsonContent.Create(userBody);

    var createRes = await http.SendAsync(createReq);

    return createRes.StatusCode switch
    {
        System.Net.HttpStatusCode.Created  => Results.Created(),
        System.Net.HttpStatusCode.Conflict => Results.Problem(title: "Username or email already exists", statusCode: 409),
        _                                  => Results.Problem(title: "Could not create user", statusCode: 400)
    };
}).AllowAnonymous();

// POST /auth/logout  →  revokes refresh token (requires valid access token)
auth.MapPost("/logout", async (
    LogoutRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url  = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/logout";
    var form = new Dictionary<string, string>
    {
        ["client_id"]     = cfg["Keycloak:ClientId"]!,
        ["refresh_token"] = req.RefreshToken
    };

    await factory.CreateClient("keycloak")
                 .PostAsync(url, new FormUrlEncodedContent(form));

    return Results.NoContent();
}).RequireAuthorization();

app.MapReverseProxy();

app.Run();



