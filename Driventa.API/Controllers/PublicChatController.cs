using Driventa.API.Hubs;
using Driventa.Application.DTOs.Common;
using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/chat")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly UserManager<ApplicationUser> _userManager;

    public PublicChatController(
        AppDbContext context,
        INotificationService notificationService,
        IHubContext<DashboardHub> dashboardHub,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _dashboardHub = dashboardHub;
        _userManager = userManager;
    }

    [HttpPost("session")]
    public async Task<ActionResult<ApiResponse<ChatSessionResponse>>> CreateSession(
        [FromBody] CreateChatSessionRequest request)
    {
        var conversation = new Conversation
        {
            VisitorId = Guid.NewGuid().ToString("N"),
            VisitorName = request.VisitorName,
            VisitorEmail = request.VisitorEmail,
            VisitorPhone = request.VisitorPhone,
            IsActive = true,
            StartedAt = DateTimeOffset.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // --- Notify all admin/dispatch users of new chat session ---
        var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
        foreach (var roleName in adminRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    NotificationType.NewMessage,
                    "New Chat Session",
                    $"{request.VisitorName} has started a new chat session.",
                    "Conversation",
                    conversation.Id);
            }
        }

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Conversation",
            action = "Created",
            entity = new
            {
                conversationId = conversation.Id,
                visitorName = conversation.VisitorName,
                visitorEmail = conversation.VisitorEmail,
                startedAt = conversation.StartedAt
            }
        });

        var response = new ChatSessionResponse
        {
            ConversationId = conversation.Id,
            VisitorId = conversation.VisitorId
        };

        return Ok(ApiResponse<ChatSessionResponse>.Ok(response, "Chat session created."));
    }
}

public class CreateChatSessionRequest
{
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorEmail { get; set; }
    public string? VisitorPhone { get; set; }
}

public class ChatSessionResponse
{
    public Guid ConversationId { get; set; }
    public string VisitorId { get; set; } = string.Empty;
}
