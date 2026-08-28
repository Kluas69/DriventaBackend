using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Messages;

public class ConversationResponse
{
    public Guid Id { get; set; }
    public string VisitorId { get; set; } = string.Empty;
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorEmail { get; set; }
    public string? VisitorPhone { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public SenderType SenderType { get; set; }
    public Guid? SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class StartChatSessionRequest
{
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorEmail { get; set; }
    public string? VisitorPhone { get; set; }
}