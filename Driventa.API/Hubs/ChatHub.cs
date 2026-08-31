using System.Security.Claims;
using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _dbContext;
    private readonly INotificationBroadcaster _broadcaster;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext dbContext, INotificationBroadcaster broadcaster, ILogger<ChatHub> logger)
    {
        _dbContext = dbContext;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task JoinConversation(string conversationId)
    {
        var conversationGuid = Guid.Parse(conversationId);
        var conversation = await _dbContext.Conversations.FindAsync(conversationGuid);

        if (conversation == null)
            throw new HubException("Conversation not found.");

        if (!conversation.IsActive)
            throw new HubException("Conversation is no longer active.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation("User {ConnectionId} joined conversation {ConversationId}", Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(string conversationId, string message)
    {
        var conversationGuid = Guid.Parse(conversationId);
        var conversation = await _dbContext.Conversations.FindAsync(conversationGuid);

        if (conversation == null)
            throw new HubException("Conversation not found.");

        if (!conversation.IsActive)
            throw new HubException("Conversation is no longer active.");

        var userId = GetUserId();

        var msg = new Message
        {
            ConversationId = conversationGuid,
            SenderType = userId.HasValue ? SenderType.Admin : SenderType.Visitor,
            SenderUserId = userId,
            Content = message
        };

        _dbContext.Messages.Add(msg);
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            messageId = msg.Id,
            message = msg.Content,
            senderUserId = msg.SenderUserId,
            senderType = msg.SenderType,
            timestamp = msg.CreatedAt
        });

        // --- Notify assigned admin when visitor sends a message ---
        if (!userId.HasValue && conversation.AssignedToUserId.HasValue)
        {
            var preview = message.Length > 100 ? message[..100] + "..." : message;
            await _broadcaster.SendToUserAsync(
                conversation.AssignedToUserId.Value,
                "New Message",
                $"{conversation.VisitorName}: {preview}",
                "Conversation",
                conversation.Id);
        }

        _logger.LogInformation("Message sent in conversation {ConversationId}", conversationId);
    }

    public async Task MarkAsRead(string conversationId)
    {
        var conversationGuid = Guid.Parse(conversationId);

        var unreadMessages = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationGuid && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }

        await _dbContext.SaveChangesAsync();
        await Clients.Group(conversationId).SendAsync("MessagesRead", conversationId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to ChatHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from ChatHub: {ConnectionId}", Context.ConnectionId);
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
