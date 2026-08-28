using System.Security.Claims;
using Driventa.Application.DTOs.Applications;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Notes;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApplicationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<Domain.Entities.Application>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] ApplicationStatus? status = null)
    {
        var query = _context.Applications
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a =>
                a.FullName.Contains(search) ||
                a.CompanyName.Contains(search) ||
                a.Email.Contains(search));

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<Domain.Entities.Application>>.Ok(
            new PaginatedResponse<Domain.Entities.Application>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> GetById(Guid id)
    {
        var application = await _context.Applications
            .Include(a => a.Notes)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> Update(
        Guid id,
        [FromBody] UpdateApplicationRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        if (request.FullName != null) application.FullName = request.FullName;
        if (request.Email != null) application.Email = request.Email;
        if (request.Phone != null) application.Phone = request.Phone;
        if (request.CompanyName != null) application.CompanyName = request.CompanyName;
        if (request.EquipmentType.HasValue) application.EquipmentType = request.EquipmentType.Value;
        if (request.TruckCount.HasValue) application.TruckCount = request.TruckCount.Value;
        if (request.McNumber != null) application.McNumber = request.McNumber;
        if (request.DotNumber != null) application.DotNumber = request.DotNumber;
        if (request.PreferredLanes != null) application.PreferredLanes = request.PreferredLanes;
        if (request.AdditionalDetails != null) application.AdditionalDetails = request.AdditionalDetails;
        if (request.Status.HasValue)
        {
            var oldStatus = application.Status;
            application.Status = request.Status.Value;
            if (request.Status.Value == ApplicationStatus.Contacted)
                application.ContactedAt = DateTimeOffset.UtcNow;
            else if (request.Status.Value == ApplicationStatus.Approved)
                application.ApprovedAt = DateTimeOffset.UtcNow;
            else if (request.Status.Value == ApplicationStatus.Rejected)
                application.RejectedAt = DateTimeOffset.UtcNow;

            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "StatusChange",
                EntityType = "Application",
                EntityId = id,
                Description = $"Application {application.ApplicationNumber} status changed: {oldStatus} → {request.Status.Value}"
            });
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application, "Application updated successfully."));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "SuperAdmin,Admin,DispatchManager")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> Assign(
        Guid id,
        [FromBody] AssignApplicationRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        application.AssignedToUserId = request.UserId;
        application.Status = ApplicationStatus.Reviewing;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Assign",
            EntityType = "Application",
            EntityId = id,
            Description = $"Application {application.ApplicationNumber} assigned to user {request.UserId}"
        });

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application, "Application assigned successfully."));
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<NoteResponse>>>> GetNotes(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<List<NoteResponse>>.Fail("Application not found."));

        var notes = await _context.ApplicationNotes
            .Where(n => n.ApplicationId == id && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteResponse
            {
                Id = n.Id,
                ParentId = n.ApplicationId,
                Content = n.Content,
                CreatedByUserId = n.CreatedByUserId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<NoteResponse>>.Ok(notes));
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<NoteResponse>>> AddNote(
        Guid id,
        [FromBody] ApplicationNoteRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<NoteResponse>.Fail("Application not found."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = new ApplicationNote
        {
            ApplicationId = id,
            Content = request.Content,
            CreatedByUserId = userId != null ? Guid.Parse(userId) : null
        };

        _context.ApplicationNotes.Add(note);
        await _context.SaveChangesAsync();

        var response = new NoteResponse
        {
            Id = note.Id,
            ParentId = note.ApplicationId,
            Content = note.Content,
            CreatedByUserId = note.CreatedByUserId,
            CreatedAt = note.CreatedAt
        };

        return Ok(ApiResponse<NoteResponse>.Ok(response, "Note added successfully."));
    }

    [HttpPost("{id:guid}/convert-to-carrier")]
    [Authorize(Roles = "SuperAdmin,Admin,DispatchManager")]
    public async Task<ActionResult<ApiResponse<Carrier>>> ConvertToCarrier(
        Guid id,
        [FromBody] ConvertToCarrierRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Carrier>.Fail("Application not found."));

        if (application.Status == ApplicationStatus.Rejected)
            return BadRequest(ApiResponse<Carrier>.Fail("Cannot convert a rejected application."));

        if (application.ConvertedCarrierId.HasValue)
            return BadRequest(ApiResponse<Carrier>.Fail("Application has already been converted to a carrier."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Create the carrier
            var carrier = new Carrier
            {
                CompanyName = application.CompanyName,
                ContactName = application.FullName,
                Email = application.Email,
                Phone = application.Phone,
                McNumber = application.McNumber,
                DotNumber = application.DotNumber,
                PreferredLanes = application.PreferredLanes,
                Notes = request.Notes,
                Status = CarrierStatus.Onboarding,
                AssignedDispatcherId = request.AssignedDispatcherId,
                ApplicationId = id
            };

            _context.Carriers.Add(carrier);
            await _context.SaveChangesAsync();

            // Step 2: Update application with conversion reference and status
            application.ConvertedCarrierId = carrier.Id;
            application.Status = ApplicationStatus.Onboarded;

            // Step 3: Log activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId != null ? Guid.Parse(userId) : null,
                Action = "ConvertToCarrier",
                EntityType = "Application",
                EntityId = id,
                Description = $"Application {application.ApplicationNumber} converted to carrier {carrier.CompanyName}"
            });

            // Step 4: Create notification
            _context.Notifications.Add(new Notification
            {
                UserId = userId != null ? Guid.Parse(userId) : Guid.Empty,
                Type = NotificationType.CarrierAssigned,
                Title = "Application Converted",
                Message = $"Application {application.ApplicationNumber} has been converted to carrier {carrier.CompanyName}.",
                EntityType = "Carrier",
                EntityId = carrier.Id
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<Carrier>.Ok(carrier, "Application converted to carrier successfully."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
