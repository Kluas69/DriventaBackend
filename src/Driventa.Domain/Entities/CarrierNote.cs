using Driventa.Domain.Common;

namespace Driventa.Domain.Entities;

public class CarrierNote : BaseEntity
{
    public Guid CarrierId { get; set; }
    public Carrier Carrier { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
}