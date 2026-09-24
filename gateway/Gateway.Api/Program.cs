using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Extensions;
using Gateway.Api.Auth;
using Gateway.Api.Authenticate.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddGatewaySwagger();
builder.Services.AddHttpClient("keycloak");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173", "http://localhost:7000"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        options.SwaggerEndpoint("/product-swagger/v1/swagger.json", "Product API v1");
    });
}

app.UseCors("GatewayCors");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

var auth = app.MapGroup("/auth").WithTags("Auth");

auth.MapPost("/login", async (
    LoginRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/token";
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = cfg["Keycloak:ClientId"]!,
        ["username"] = req.Username,
        ["password"] = req.Password,
        ["scope"] = "openid profile email"
    };

    var res = await factory.CreateClient("keycloak")
        .PostAsync(url, new FormUrlEncodedContent(form));

    if (!res.IsSuccessStatusCode)
        return Results.Problem(title: "Invalid credentials", statusCode: 401);

    var token = await res.Content.ReadFromJsonAsync<TokenResponse>();
    return Results.Ok(token);
}).AllowAnonymous();

auth.MapPost("/refresh", async (
    RefreshRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/token";
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "refresh_token",
        ["client_id"] = cfg["Keycloak:ClientId"]!,
        ["refresh_token"] = req.RefreshToken
    };

    var res = await factory.CreateClient("keycloak")
        .PostAsync(url, new FormUrlEncodedContent(form));

    if (!res.IsSuccessStatusCode)
        return Results.Problem(title: "Invalid or expired refresh token", statusCode: 401);

    var token = await res.Content.ReadFromJsonAsync<TokenResponse>();
    return Results.Ok(token);
}).AllowAnonymous();

auth.MapPost("/register", async (
    RegisterRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var authority = cfg["Keycloak:Authority"]!;
    var parts = authority.Split("/realms/", 2);
    var baseUrl = parts[0];
    var realm = parts[1];
    var http = factory.CreateClient("keycloak");

    var tokenUrl = $"{baseUrl}/realms/master/protocol/openid-connect/token";
    var tokenForm = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = "admin-cli",
        ["username"] = cfg["Keycloak:AdminUsername"]!,
        ["password"] = cfg["Keycloak:AdminPassword"]!
    };

    var tokenRes = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenForm));
    if (!tokenRes.IsSuccessStatusCode)
        return Results.Problem(title: "Authentication service unavailable", statusCode: 503);

    var tokenDoc = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
    var adminToken = tokenDoc.GetProperty("access_token").GetString()!;

    var createUrl = $"{baseUrl}/admin/realms/{realm}/users";
    var userBody = new
    {
        username = req.Username,
        email = req.Email,
        firstName = req.FirstName,
        lastName = req.LastName,
        enabled = true,
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
        System.Net.HttpStatusCode.Created => Results.Created(),
        System.Net.HttpStatusCode.Conflict => Results.Problem(title: "Username or email already exists", statusCode: 409),
        _ => Results.Problem(title: "Could not create user", statusCode: 400)
    };
}).AllowAnonymous();

auth.MapPost("/logout", async (
    LogoutRequest req,
    IHttpClientFactory factory,
    IConfiguration cfg) =>
{
    var url = $"{cfg["Keycloak:Authority"]}/protocol/openid-connect/logout";
    var form = new Dictionary<string, string>
    {
        ["client_id"] = cfg["Keycloak:ClientId"]!,
        ["refresh_token"] = req.RefreshToken
    };

    await factory.CreateClient("keycloak")
        .PostAsync(url, new FormUrlEncodedContent(form));

    return Results.NoContent();
}).RequireAuthorization(AuthConstants.AuthenticatedPolicy);

// Forward the inbound Authorization header to downstream services.
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authorization)
            && !string.IsNullOrWhiteSpace(authorization))
        {
            context.Request.Headers.Authorization = authorization;
        }

        await next();
    });
});

app.Run();
