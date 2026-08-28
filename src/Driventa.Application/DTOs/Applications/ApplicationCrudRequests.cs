using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Applications;

public class CreateApplicationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public int TruckCount { get; set; }
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? PreferredLanes { get; set; }
    public string? AdditionalDetails { get; set; }
}

public class UpdateApplicationRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public int? TruckCount { get; set; }
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? PreferredLanes { get; set; }
    public string? AdditionalDetails { get; set; }
    public ApplicationStatus? Status { get; set; }
}

public class AssignApplicationRequest
{
    public Guid UserId { get; set; }
}

public class ApplicationNoteRequest
{
    public string Content { get; set; } = string.Empty;
}

public class ConvertToCarrierRequest
{
    public Guid? AssignedDispatcherId { get; set; }
    public string? Notes { get; set; }
}