using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public SenderType SenderType { get; set; }
    public Guid? SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}