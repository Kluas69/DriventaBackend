using System.Security.Claims;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Messages;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("conversations")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ConversationResponse>>>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Conversations
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationResponse
            {
                Id = c.Id,
                VisitorId = c.VisitorId,
                VisitorName = c.VisitorName,
                VisitorEmail = c.VisitorEmail,
                VisitorPhone = c.VisitorPhone,
                AssignedToUserId = c.AssignedToUserId,
                IsActive = c.IsActive,
                StartedAt = c.StartedAt,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = c.Messages.Count(m => !m.IsRead),
                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                LastMessageSenderType = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (SenderType?)m.SenderType)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<ConversationResponse>>.Ok(
            new PaginatedResponse<ConversationResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("conversations/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ConversationResponse>>> GetConversationById(Guid id)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return NotFound(ApiResponse<ConversationResponse>.Fail("Conversation not found."));

        var messages = await _context.Messages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderType = m.SenderType,
                SenderUserId = m.SenderUserId,
                SenderName = m.SenderType == SenderType.Visitor
                    ? conversation.VisitorName
                    : m.SenderUserId.HasValue
                        ? m.SenderUserId.Value.ToString()
                        : "System",
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        var unreadCount = messages.Count(m => !m.IsRead);

        var lastMsg = messages.LastOrDefault();

        var response = new ConversationResponse
        {
            Id = conversation.Id,
            VisitorId = conversation.VisitorId,
            VisitorName = conversation.VisitorName,
            VisitorEmail = conversation.VisitorEmail,
            VisitorPhone = conversation.VisitorPhone,
            AssignedToUserId = conversation.AssignedToUserId,
            IsActive = conversation.IsActive,
            StartedAt = conversation.StartedAt,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            LastMessage = lastMsg?.Content,
            LastMessageSenderType = lastMsg?.SenderType,
            Messages = messages
        };

        return Ok(ApiResponse<ConversationResponse>.Ok(response));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> SendMessage([FromBody] SendMessageRequest request)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId);

        if (conversation == null)
            return NotFound(ApiResponse<MessageResponse>.Fail("Conversation not found."));

        if (!conversation.IsActive)
            return BadRequest(ApiResponse<MessageResponse>.Fail("Conversation is no longer active."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderType = SenderType.Admin,
            SenderUserId = userId != null ? Guid.Parse(userId) : null,
            Content = request.Content,
            IsRead = false
        };

        _context.Messages.Add(message);

        conversation.LastMessageAt = DateTimeOffset.UtcNow;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "SendMessage",
            EntityType = "Conversation",
            EntityId = request.ConversationId,
            Description = $"Message sent in conversation with visitor {conversation.VisitorName}"
        });

        await _context.SaveChangesAsync();

        var response = new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderType = message.SenderType,
            SenderUserId = message.SenderUserId,
            SenderName = userName,
            Content = message.Content,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };

        return Ok(ApiResponse<MessageResponse>.Ok(response, "Message sent successfully."));
    }

    [HttpPatch("conversations/{id:guid}/read")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> MarkConversationAsRead(Guid id)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return NotFound(ApiResponse<object>.Fail("Conversation not found."));

        var unreadMessages = await _context.Messages
            .Where(m => m.ConversationId == id && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new object(), "Conversation marked as read."));
    }
}
