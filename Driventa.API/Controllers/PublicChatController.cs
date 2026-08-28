using Driventa.Application.DTOs.Common;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/chat")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicChatController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicChatController(AppDbContext context)
    {
        _context = context;
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
