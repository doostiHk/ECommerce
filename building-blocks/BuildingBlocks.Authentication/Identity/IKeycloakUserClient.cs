namespace BuildingBlocks.Authentication.Identity;

public interface IKeycloakUserClient
{
    Task<KeycloakUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, KeycloakUser>> GetUsersByIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}
