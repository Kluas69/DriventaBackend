using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentType DocumentType { get; set; }
    public Guid? CarrierId { get; set; }
    public Carrier? Carrier { get; set; }
    public Guid? LoadId { get; set; }
    public Load? Load { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}