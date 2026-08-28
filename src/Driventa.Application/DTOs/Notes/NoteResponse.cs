namespace Driventa.Application.DTOs.Notes;

public class NoteResponse
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
