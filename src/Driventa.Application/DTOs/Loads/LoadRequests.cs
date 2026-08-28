using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Loads;

public class CreateLoadRequest
{
    public Guid CarrierId { get; set; }
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? BrokerId { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;
    public DateTimeOffset PickupDateTime { get; set; }
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public DateTimeOffset DeliveryDateTime { get; set; }
    public decimal Rate { get; set; }
    public int? Miles { get; set; }
    public string? DispatchFeeType { get; set; }
    public decimal? DispatchFeeValue { get; set; }
}

public class UpdateLoadRequest
{
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? BrokerId { get; set; }
    public string? PickupCity { get; set; }
    public string? PickupState { get; set; }
    public DateTimeOffset? PickupDateTime { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public DateTimeOffset? DeliveryDateTime { get; set; }
    public decimal? Rate { get; set; }
    public int? Miles { get; set; }
    public string? DispatchFeeType { get; set; }
    public decimal? DispatchFeeValue { get; set; }
}

public class LoadStatusUpdateRequest
{
    public LoadStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class LoadNoteRequest
{
    public string Content { get; set; } = string.Empty;
}

public class LoadResponse
{
    public Guid Id { get; set; }
    public string LoadNumber { get; set; } = string.Empty;
    public Guid CarrierId { get; set; }
    public string? CarrierName { get; set; }
    public Guid? TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? BrokerId { get; set; }
    public string? BrokerName { get; set; }
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
    public decimal? DispatchFeeAmount { get; set; }
    public decimal? CarrierNetAmount { get; set; }
    public LoadStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}