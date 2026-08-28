using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Carriers;

public class CreateCarrierRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PreferredLanes { get; set; }
    public string? Notes { get; set; }
    public Guid? ApplicationId { get; set; }
}

public class UpdateCarrierRequest
{
    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public CarrierStatus? Status { get; set; }
    public string? PreferredLanes { get; set; }
    public string? Notes { get; set; }
}

public class AssignDispatcherRequest
{
    public Guid DispatcherId { get; set; }
}

public class CarrierResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? McNumber { get; set; }
    public string? DotNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public CarrierStatus Status { get; set; }
    public Guid? AssignedDispatcherId { get; set; }
    public string? PreferredLanes { get; set; }
    public string? Notes { get; set; }
    public Guid? ApplicationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}