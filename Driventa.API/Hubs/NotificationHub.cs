using System.Security.Claims;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(AppDbContext dbContext, ILogger<NotificationHub> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task JoinPersonalGroup()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            var groupName = $"user_{userId.Value}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined notification group", userId.Value);
        }
    }

    public async Task SendNotificationToUser(Guid targetUserId, string title, string message)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue || senderId.Value != targetUserId)
        {
            throw new HubException("You can only send notifications to yourself.");
        }

        await Clients.Group($"user_{targetUserId}").SendAsync("ReceiveNotification", new
        {
            title,
            message,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Clients.Caller.SendAsync("Connected", new { userId = userId.Value });
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from NotificationHub: {ConnectionId}", Context.ConnectionId);
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
