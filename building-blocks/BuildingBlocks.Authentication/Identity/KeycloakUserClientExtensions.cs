using BuildingBlocks.Contracts.Users;

namespace BuildingBlocks.Authentication.Identity;

public static class KeycloakUserClientExtensions
{
    public static async Task<IReadOnlyDictionary<string, UserDisplayInfo>> GetDisplayNamesByIdsAsync(
        this IKeycloakUserClient client,
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var users = await client.GetUsersByIdsAsync(
            userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!),
            cancellationToken);

        return users.ToDictionary(
            pair => pair.Key,
            pair => new UserDisplayInfo(pair.Value.Id, pair.Value.DisplayName),
            StringComparer.Ordinal);
    }
}
