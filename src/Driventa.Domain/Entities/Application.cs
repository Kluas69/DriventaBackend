using Driventa.Domain.Common;
using Driventa.Domain.Enums;

namespace Driventa.Domain.Entities;

public class Application : BaseEntity
{
    public string ApplicationNumber { get; set; } = string.Empty;
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
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ContactedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public Guid? ConvertedCarrierId { get; set; }
    public Carrier? ConvertedCarrier { get; set; }
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
}