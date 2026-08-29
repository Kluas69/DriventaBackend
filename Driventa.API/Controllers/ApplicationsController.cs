using System.Security.Claims;
using Driventa.Application.DTOs.Applications;
using Driventa.Application.DTOs.Common;
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
    [Authorize(Policy = "applications.view")]
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
    [Authorize(Policy = "applications.view")]
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
    [Authorize(Policy = "applications.edit")]
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
    [Authorize(Policy = "applications.assign")]
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

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<ApplicationNote>>> AddNote(
        Guid id,
        [FromBody] ApplicationNoteRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationNote>.Fail("Application not found."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = new ApplicationNote
        {
            ApplicationId = id,
            Content = request.Content,
            CreatedByUserId = userId != null ? Guid.Parse(userId) : null
        };

        _context.ApplicationNotes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ApplicationNote>.Ok(note, "Note added successfully."));
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<List<ApplicationNote>>>> GetNotes(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<List<ApplicationNote>>.Fail("Application not found."));

        var notes = await _context.ApplicationNotes
            .Where(n => n.ApplicationId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<ApplicationNote>>.Ok(notes));
    }

    [HttpPost("{id:guid}/convert-to-carrier")]
    [Authorize(Policy = "applications.convert")]
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

    [HttpPost("{id:guid}/contact")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> Contact(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        application.Status = ApplicationStatus.Contacted;
        application.ContactedAt = DateTimeOffset.UtcNow;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Contact",
            EntityType = "Application",
            EntityId = id,
            Description = $"Application {application.ApplicationNumber} marked as contacted"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application, "Application marked as contacted."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> Approve(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        application.Status = ApplicationStatus.Approved;
        application.ApprovedAt = DateTimeOffset.UtcNow;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Approve",
            EntityType = "Application",
            EntityId = id,
            Description = $"Application {application.ApplicationNumber} approved"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application, "Application approved successfully."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<Domain.Entities.Application>>> Reject(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<Domain.Entities.Application>.Fail("Application not found."));

        application.Status = ApplicationStatus.Rejected;
        application.RejectedAt = DateTimeOffset.UtcNow;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Reject",
            EntityType = "Application",
            EntityId = id,
            Description = $"Application {application.ApplicationNumber} rejected"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Domain.Entities.Application>.Ok(application, "Application rejected."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<object>.Fail("Application not found."));

        application.IsDeleted = true;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Delete",
            EntityType = "Application",
            EntityId = id,
            Description = $"Application {application.ApplicationNumber} deleted"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new object(), "Application deleted successfully."));
    }
}
