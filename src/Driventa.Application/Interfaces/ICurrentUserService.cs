using System.Security.Claims;

namespace Driventa.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? GetUserId();
    string? GetUserRole();
    ClaimsPrincipal? GetUser();
}