using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Documents;

public class DocumentResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentType DocumentType { get; set; }
    public Guid? CarrierId { get; set; }
    public Guid? LoadId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}