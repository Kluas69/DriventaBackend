using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Trucks;

public class CreateTruckRequest
{
    public Guid CarrierId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
}

public class UpdateTruckRequest
{
    public string? TruckNumber { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
    public TruckStatus? Status { get; set; }
}

public class TruckResponse
{
    public Guid Id { get; set; }
    public Guid CarrierId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
    public TruckStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}