namespace BuildingBlocks.Authentication.Options;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty;
    public string? MetadataAddress { get; set; }
    public string? ClientId { get; set; }
    public bool RequireHttpsMetadata { get; set; }
    public string[] ValidIssuers { get; set; } = [];
    public string AdminRole { get; set; } = "admin";
}

public sealed class KeycloakAdminOptions
{
    public const string SectionName = "Keycloak:Admin";

    /// <summary>Keycloak base URL without /realms/... (e.g. http://keycloak:8080).</summary>
    public string? BaseUrl { get; set; }

    public string Realm { get; set; } = "Core";
    public string ClientId { get; set; } = "product-service";
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Optional fallback when client credentials are unavailable.</summary>
    public string? Username { get; set; }
    public string? Password { get; set; }

    public int CacheSeconds { get; set; } = 300;
}
