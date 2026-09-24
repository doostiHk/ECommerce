namespace BuildingBlocks.Authentication.Identity;

public sealed record KeycloakUser(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled)
{
    public string DisplayName
    {
        get
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? Username : fullName;
        }
    }
}
