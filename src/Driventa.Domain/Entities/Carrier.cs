using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Carrier : BaseEntity
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
    public CarrierStatus Status { get; set; } = CarrierStatus.Lead;
    public Guid? AssignedDispatcherId { get; set; }
    public string? PreferredLanes { get; set; }
    public string? Notes { get; set; }
    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }
    public ICollection<Truck> Trucks { get; set; } = new List<Truck>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Load> Loads { get; set; } = new List<Load>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<CarrierNote> CarrierNotes { get; set; } = new List<CarrierNote>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}