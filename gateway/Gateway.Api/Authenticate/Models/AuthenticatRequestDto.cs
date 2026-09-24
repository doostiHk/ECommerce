using System.Text.Json.Serialization;

namespace Gateway.Api.Authenticate.Models;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password, string? FirstName, string? LastName);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("refresh_expires_in")] int? RefreshExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("scope")] string? Scope
);
