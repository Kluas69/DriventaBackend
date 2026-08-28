using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Driver : BaseEntity
{
    public Guid CarrierId { get; set; }
    public Carrier Carrier { get; set; } = null!;
    public Guid? TruckId { get; set; }
    public Truck? Truck { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseState { get; set; }
    public DriverStatus Status { get; set; } = DriverStatus.Available;
}