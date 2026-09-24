using System.Security.Claims;
using System.Text.Json;

namespace BuildingBlocks.Authentication.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue("sub")
        ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetUserName(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue("preferred_username")
        ?? principal?.Identity?.Name
        ?? principal?.FindFirstValue(ClaimTypes.Name);

    public static void MapKeycloakRoles(this ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrWhiteSpace(realmAccess))
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var value = role.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        identity.AddClaim(new Claim(ClaimTypes.Role, value));
                }
            }
        }

        var resourceAccess = identity.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccess))
            return;

        using var resourceDoc = JsonDocument.Parse(resourceAccess);
        foreach (var client in resourceDoc.RootElement.EnumerateObject())
        {
            if (!client.Value.TryGetProperty("roles", out var clientRoles))
                continue;

            foreach (var role in clientRoles.EnumerateArray())
            {
                var value = role.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, value));
                    identity.AddClaim(new Claim(ClaimTypes.Role, $"{client.Name}:{value}"));
                }
            }
        }
    }
}
