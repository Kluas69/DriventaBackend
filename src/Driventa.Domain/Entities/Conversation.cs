using Driventa.Domain.Common;

namespace Driventa.Domain.Entities;

public class Conversation : BaseEntity
{
    public string VisitorId { get; set; } = string.Empty;
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorEmail { get; set; }
    public string? VisitorPhone { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastMessageAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}