using System.Security.Claims;
using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Authentication.Extensions;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => Principal.GetUserId();

    public string? UserName => Principal.GetUserName();

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray()
        ?? [];

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
