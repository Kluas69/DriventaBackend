using Driventa.Domain.Entities;
using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Applications;

public class ApplicationDto
{
    public Guid Id { get; set; }
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
    public ApplicationStatus Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ContactedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public Guid? ConvertedCarrierId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public List<ApplicationNoteDto> Notes { get; set; } = new();

    public static ApplicationDto FromEntity(Domain.Entities.Application application)
    {
        return new ApplicationDto
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            FullName = application.FullName,
            Email = application.Email,
            Phone = application.Phone,
            CompanyName = application.CompanyName,
            EquipmentType = application.EquipmentType,
            TruckCount = application.TruckCount,
            McNumber = application.McNumber,
            DotNumber = application.DotNumber,
            PreferredLanes = application.PreferredLanes,
            AdditionalDetails = application.AdditionalDetails,
            Status = application.Status,
            AssignedToUserId = application.AssignedToUserId,
            SubmittedAt = application.SubmittedAt,
            ContactedAt = application.ContactedAt,
            ApprovedAt = application.ApprovedAt,
            RejectedAt = application.RejectedAt,
            ConvertedCarrierId = application.ConvertedCarrierId,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            CreatedByUserId = application.CreatedByUserId,
            UpdatedByUserId = application.UpdatedByUserId,
            Notes = application.Notes.Select(ApplicationNoteDto.FromEntity).ToList()
        };
    }
}

public class ApplicationNoteDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public static ApplicationNoteDto FromEntity(ApplicationNote note)
    {
        return new ApplicationNoteDto
        {
            Id = note.Id,
            ApplicationId = note.ApplicationId,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            CreatedByUserId = note.CreatedByUserId,
            UpdatedByUserId = note.UpdatedByUserId
        };
    }
}
