using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Driventa.API.Hubs;

[Authorize]
public class ApplicationsHub : Hub
{
    private readonly ILogger<ApplicationsHub> _logger;

    public ApplicationsHub(ILogger<ApplicationsHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        _logger.LogInformation("User {ConnectionId} joined admin group", Context.ConnectionId);
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
        _logger.LogInformation("User {ConnectionId} left admin group", Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _logger.LogInformation("Client connected to ApplicationsHub: {ConnectionId} (User: {UserId})", Context.ConnectionId, userId.Value);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from ApplicationsHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }
}
