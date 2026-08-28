using Driventa.Domain.Common;

namespace Driventa.Domain.Entities;

public class ApplicationNote : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
}