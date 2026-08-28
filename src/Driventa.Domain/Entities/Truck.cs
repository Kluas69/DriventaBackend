using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Truck : BaseEntity
{
    public Guid CarrierId { get; set; }
    public Carrier Carrier { get; set; } = null!;
    public string TruckNumber { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
    public TruckStatus Status { get; set; } = TruckStatus.Available;
}