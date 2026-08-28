using Driventa.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Driventa.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }

    public string? GetUserRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    }

    public System.Security.Claims.ClaimsPrincipal? GetUser()
    {
        return _httpContextAccessor.HttpContext?.User;
    }
}