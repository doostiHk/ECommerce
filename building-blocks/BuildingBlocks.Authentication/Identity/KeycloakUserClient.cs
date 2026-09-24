using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Authentication.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication.Identity;

public sealed class KeycloakUserClient(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<KeycloakOptions> keycloakOptions,
    IOptions<KeycloakAdminOptions> adminOptions,
    ILogger<KeycloakUserClient> logger) : IKeycloakUserClient
{
    private const string HttpClientName = "keycloak-admin";
    private const string TokenCacheKey = "keycloak-admin-token";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    public async Task<KeycloakUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var map = await GetUsersByIdsAsync([userId], cancellationToken);
        return map.GetValueOrDefault(userId);
    }

    public async Task<IReadOnlyDictionary<string, KeycloakUser>> GetUsersByIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<string, KeycloakUser>(StringComparer.Ordinal);

        var result = new Dictionary<string, KeycloakUser>(StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var id in ids)
        {
            if (cache.TryGetValue(UserCacheKey(id), out KeycloakUser? cached) && cached is not null)
                result[id] = cached;
            else
                missing.Add(id);
        }

        if (missing.Count == 0)
            return result;

        var options = adminOptions.Value;
        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
            return result;

        var baseUrl = ResolveBaseUrl(options, keycloakOptions.Value);
        var http = httpClientFactory.CreateClient(HttpClientName);

        await Parallel.ForEachAsync(
            missing,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
            async (userId, ct) =>
            {
                var gate = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(ct);
                try
                {
                    if (cache.TryGetValue(UserCacheKey(userId), out KeycloakUser? cached) && cached is not null)
                    {
                        lock (result)
                            result[userId] = cached;
                        return;
                    }

                    var url = $"{baseUrl}/admin/realms/{options.Realm}/users/{Uri.EscapeDataString(userId)}";
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var response = await http.SendAsync(request, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning(
                            "Keycloak user lookup failed for {UserId}. Status={StatusCode}",
                            userId,
                            (int)response.StatusCode);
                        return;
                    }

                    var dto = await response.Content.ReadFromJsonAsync<KeycloakUserDto>(cancellationToken: ct);
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Id))
                        return;

                    var user = new KeycloakUser(
                        dto.Id,
                        dto.Username ?? dto.Id,
                        dto.Email,
                        dto.FirstName,
                        dto.LastName,
                        dto.Enabled);

                    cache.Set(
                        UserCacheKey(user.Id),
                        user,
                        TimeSpan.FromSeconds(Math.Max(30, options.CacheSeconds)));

                    lock (result)
                        result[user.Id] = user;
                }
                finally
                {
                    gate.Release();
                }
            });

        return result;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            return cachedToken;

        var options = adminOptions.Value;
        var keycloak = keycloakOptions.Value;
        var baseUrl = ResolveBaseUrl(options, keycloak);
        var http = httpClientFactory.CreateClient(HttpClientName);

        Dictionary<string, string> form;
        string tokenUrl;

        if (!string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            tokenUrl = $"{baseUrl}/realms/{options.Realm}/protocol/openid-connect/token";
            form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret
            };
        }
        else if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
        {
            // Master-realm admin-cli fallback for local/dev.
            tokenUrl = $"{baseUrl}/realms/master/protocol/openid-connect/token";
            form = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = options.Username!,
                ["password"] = options.Password!
            };
        }
        else
        {
            logger.LogWarning("Keycloak admin credentials are not configured. User enrichment will be skipped.");
            return null;
        }

        using var response = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to obtain Keycloak admin token. Status={StatusCode}", (int)response.StatusCode);
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: cancellationToken);
        if (token?.AccessToken is null)
            return null;

        var lifetime = TimeSpan.FromSeconds(Math.Max(30, token.ExpiresIn - 30));
        cache.Set(TokenCacheKey, token.AccessToken, lifetime);
        return token.AccessToken;
    }

    private static string ResolveBaseUrl(KeycloakAdminOptions admin, KeycloakOptions keycloak)
    {
        if (!string.IsNullOrWhiteSpace(admin.BaseUrl))
            return admin.BaseUrl.TrimEnd('/');

        var authority = keycloak.Authority;
        var marker = "/realms/";
        var index = authority.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index > 0 ? authority[..index].TrimEnd('/') : authority.TrimEnd('/');
    }

    private static string UserCacheKey(string userId) => $"keycloak-user:{userId}";

    private sealed class KeycloakUserDto
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; }
    }

    private sealed class TokenDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 60;
    }
}
