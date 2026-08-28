using Driventa.Domain.Common;

namespace Driventa.Domain.Entities;

public class LoadNote : BaseEntity
{
    public Guid LoadId { get; set; }
    public Load Load { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
}