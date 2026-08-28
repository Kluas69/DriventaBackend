using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Load : BaseEntity
{
    public string LoadNumber { get; set; } = string.Empty;
    public Guid CarrierId { get; set; }
    public Carrier Carrier { get; set; } = null!;
    public Guid? TruckId { get; set; }
    public Truck? Truck { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid? BrokerId { get; set; }
    public Broker? Broker { get; set; }
    public Guid? DispatcherId { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;
    public DateTimeOffset PickupDateTime { get; set; }
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public DateTimeOffset DeliveryDateTime { get; set; }
    public decimal Rate { get; set; }
    public int? Miles { get; set; }
    public decimal? RatePerMile { get; set; }
    public string? DispatchFeeType { get; set; }
    public decimal? DispatchFeeValue { get; set; }
    public decimal? DispatchFeeAmount { get; set; }
    public decimal? CarrierNetAmount { get; set; }
    public LoadStatus Status { get; set; } = LoadStatus.Available;
    public DateTimeOffset? BookedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<LoadNote> LoadNotes { get; set; } = new List<LoadNote>();
}