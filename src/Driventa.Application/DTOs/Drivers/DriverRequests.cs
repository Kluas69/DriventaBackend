using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Drivers;

public class CreateDriverRequest
{
    public Guid CarrierId { get; set; }
    public Guid? TruckId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseState { get; set; }
}

public class UpdateDriverRequest
{
    public Guid? TruckId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseState { get; set; }
    public DriverStatus? Status { get; set; }
}

public class DriverResponse
{
    public Guid Id { get; set; }
    public Guid CarrierId { get; set; }
    public Guid? TruckId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseState { get; set; }
    public DriverStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}