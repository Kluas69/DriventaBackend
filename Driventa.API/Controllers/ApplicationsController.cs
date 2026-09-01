using System.Security.Claims;
using Driventa.API.Hubs;
using Driventa.Application.DTOs.Applications;
using Driventa.Application.DTOs.Carriers;
using Driventa.Application.DTOs.Common;
using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<ApplicationsHub> _applicationsHub;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(
        AppDbContext context,
        INotificationService notificationService,
        IHubContext<ApplicationsHub> applicationsHub,
        IHubContext<DashboardHub> dashboardHub,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _applicationsHub = applicationsHub;
        _dashboardHub = dashboardHub;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ApplicationDto>>>> GetAll(
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

        return Ok(ApiResponse<PaginatedResponse<ApplicationDto>>.Ok(
            new PaginatedResponse<ApplicationDto>
            {
                Items = items.Select(ApplicationDto.FromEntity).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> GetById(Guid id)
    {
        var application = await _context.Applications
            .Include(a => a.Notes)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> Update(
        Guid id,
        [FromBody] UpdateApplicationRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

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

            // --- Status change notifications ---
            var statusChangeData = new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber,
                companyName = application.CompanyName,
                fullName = application.FullName,
                oldStatus = oldStatus.ToString(),
                newStatus = request.Status.Value.ToString(),
                timestamp = DateTimeOffset.UtcNow
            };

            await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationStatusChanged", statusChangeData);
            await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
            {
                entityType = "Application",
                action = "StatusChanged",
                entity = statusChangeData
            });

            // Notify assigned user, or all admins as fallback
            if (application.AssignedToUserId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    application.AssignedToUserId.Value,
                    NotificationType.ApplicationStatusChanged,
                    "Application Status Changed",
                    $"Application {application.ApplicationNumber} status changed: {oldStatus} → {request.Status.Value}",
                    "Application",
                    application.Id);
            }
            else
            {
                var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
                foreach (var roleName in adminRoles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                    foreach (var user in usersInRole)
                    {
                        await _notificationService.CreateNotificationAsync(
                            user.Id,
                            NotificationType.ApplicationStatusChanged,
                            "Application Status Changed",
                            $"Application {application.ApplicationNumber} ({application.CompanyName}) status changed: {oldStatus} → {request.Status.Value}",
                            "Application",
                            application.Id);
                    }
                }
            }
        }

        // --- General update broadcast ---
        var updateData = new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            companyName = application.CompanyName,
            fullName = application.FullName,
            status = application.Status.ToString()
        };

        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationUpdated", updateData);
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "Updated",
            entity = updateData
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application), "Application updated successfully."));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = "applications.assign")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> Assign(
        Guid id,
        [FromBody] AssignApplicationRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

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

        // --- Assignment notifications ---
        var assignData = new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            companyName = application.CompanyName,
            assignedToUserId = request.UserId,
            timestamp = DateTimeOffset.UtcNow
        };

        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationUpdated", assignData);
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "Assigned",
            entity = assignData
        });

        // Notify the assigned user
        await _notificationService.CreateNotificationAsync(
            request.UserId,
            NotificationType.ApplicationAssigned,
            "Application Assigned",
            $"Application {application.ApplicationNumber} ({application.CompanyName}) has been assigned to you.",
            "Application",
            application.Id);

        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application), "Application assigned successfully."));
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<ApplicationNoteDto>>> AddNote(
        Guid id,
        [FromBody] ApplicationNoteRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationNoteDto>.Fail("Application not found."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = new ApplicationNote
        {
            ApplicationId = id,
            Content = request.Content,
            CreatedByUserId = userId != null ? Guid.Parse(userId) : null
        };

        _context.ApplicationNotes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ApplicationNoteDto>.Ok(ApplicationNoteDto.FromEntity(note), "Note added successfully."));
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize(Policy = "applications.view")]
    public async Task<ActionResult<ApiResponse<List<ApplicationNoteDto>>>> GetNotes(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<List<ApplicationNoteDto>>.Fail("Application not found."));

        var notes = await _context.ApplicationNotes
            .Where(n => n.ApplicationId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<ApplicationNoteDto>>.Ok(notes.Select(ApplicationNoteDto.FromEntity).ToList()));
    }

    [HttpPost("{id:guid}/convert-to-carrier")]
    [Authorize(Policy = "applications.convert")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> ConvertToCarrier(
        Guid id,
        [FromBody] ConvertToCarrierRequest request)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<CarrierResponse>.Fail("Application not found."));

        if (application.Status == ApplicationStatus.Rejected)
            return BadRequest(ApiResponse<CarrierResponse>.Fail("Cannot convert a rejected application."));

        if (application.ConvertedCarrierId.HasValue)
            return BadRequest(ApiResponse<CarrierResponse>.Fail("Application has already been converted to a carrier."));

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

            // Step 4: Create and push realtime notification to all admin/dispatch users
            var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
            foreach (var roleName in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                foreach (var user in usersInRole)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        NotificationType.CarrierAssigned,
                        "Application Converted",
                        $"Application {application.ApplicationNumber} has been converted to carrier {carrier.CompanyName}.",
                        "Carrier",
                        carrier.Id);
                }
            }

            // Broadcast to admins
            await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationStatusChanged", new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber,
                companyName = application.CompanyName,
                oldStatus = "Onboarded",
                newStatus = "Onboarded",
                timestamp = DateTimeOffset.UtcNow
            });

            await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
            {
                entityType = "Application",
                action = "ConvertedToCarrier",
                entity = new
                {
                    applicationId = application.Id,
                    applicationNumber = application.ApplicationNumber,
                    carrierId = carrier.Id,
                    carrierName = carrier.CompanyName
                }
            });

            await transaction.CommitAsync();

            return Ok(ApiResponse<CarrierResponse>.Ok(new CarrierResponse
            {
                Id = carrier.Id,
                CompanyName = carrier.CompanyName,
                ContactName = carrier.ContactName,
                Email = carrier.Email,
                Phone = carrier.Phone,
                McNumber = carrier.McNumber,
                DotNumber = carrier.DotNumber,
                PreferredLanes = carrier.PreferredLanes,
                Notes = carrier.Notes,
                Status = carrier.Status,
                AssignedDispatcherId = carrier.AssignedDispatcherId,
                ApplicationId = carrier.ApplicationId,
                CreatedAt = carrier.CreatedAt
            }, "Application converted to carrier successfully."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{id:guid}/contact")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> Contact(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

        var oldStatus = application.Status;
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

        // Notify assigned user, or all admins as fallback
        if (application.AssignedToUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                application.AssignedToUserId.Value,
                NotificationType.ApplicationStatusChanged,
                "Application Contacted",
                $"Application {application.ApplicationNumber} has been marked as contacted.",
                "Application",
                application.Id);
        }
        else
        {
            var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
            foreach (var roleName in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                foreach (var user in usersInRole)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        NotificationType.ApplicationStatusChanged,
                        "Application Contacted",
                        $"Application {application.ApplicationNumber} ({application.CompanyName}) has been marked as contacted.",
                        "Application",
                        application.Id);
                }
            }
        }

        // Broadcast
        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationStatusChanged", new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            oldStatus = oldStatus.ToString(),
            newStatus = ApplicationStatus.Contacted.ToString(),
            timestamp = DateTimeOffset.UtcNow
        });

        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "StatusChanged",
            entity = new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber,
                oldStatus = oldStatus.ToString(),
                newStatus = ApplicationStatus.Contacted.ToString()
            }
        });

        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application), "Application marked as contacted."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> Approve(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

        var oldStatus = application.Status;
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

        // Notify assigned user, or all admins as fallback
        if (application.AssignedToUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                application.AssignedToUserId.Value,
                NotificationType.ApplicationStatusChanged,
                "Application Approved",
                $"Application {application.ApplicationNumber} has been approved.",
                "Application",
                application.Id);
        }
        else
        {
            var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
            foreach (var roleName in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                foreach (var user in usersInRole)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        NotificationType.ApplicationStatusChanged,
                        "Application Approved",
                        $"Application {application.ApplicationNumber} ({application.CompanyName}) has been approved.",
                        "Application",
                        application.Id);
                }
            }
        }

        // Broadcast
        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationStatusChanged", new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            oldStatus = oldStatus.ToString(),
            newStatus = ApplicationStatus.Approved.ToString(),
            timestamp = DateTimeOffset.UtcNow
        });

        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "StatusChanged",
            entity = new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber,
                oldStatus = oldStatus.ToString(),
                newStatus = ApplicationStatus.Approved.ToString()
            }
        });

        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application), "Application approved successfully."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "applications.edit")]
    public async Task<ActionResult<ApiResponse<ApplicationDto>>> Reject(Guid id)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (application == null)
            return NotFound(ApiResponse<ApplicationDto>.Fail("Application not found."));

        var oldStatus = application.Status;
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

        // Notify assigned user, or all admins as fallback
        if (application.AssignedToUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                application.AssignedToUserId.Value,
                NotificationType.ApplicationStatusChanged,
                "Application Rejected",
                $"Application {application.ApplicationNumber} has been rejected.",
                "Application",
                application.Id);
        }
        else
        {
            var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
            foreach (var roleName in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                foreach (var user in usersInRole)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        NotificationType.ApplicationStatusChanged,
                        "Application Rejected",
                        $"Application {application.ApplicationNumber} ({application.CompanyName}) has been rejected.",
                        "Application",
                        application.Id);
                }
            }
        }

        // Broadcast
        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationStatusChanged", new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            oldStatus = oldStatus.ToString(),
            newStatus = ApplicationStatus.Rejected.ToString(),
            timestamp = DateTimeOffset.UtcNow
        });

        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "StatusChanged",
            entity = new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber,
                oldStatus = oldStatus.ToString(),
                newStatus = ApplicationStatus.Rejected.ToString()
            }
        });

        return Ok(ApiResponse<ApplicationDto>.Ok(ApplicationDto.FromEntity(application), "Application rejected."));
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

        // Broadcast deletion
        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationDeleted", new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            timestamp = DateTimeOffset.UtcNow
        });

        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "Deleted",
            entity = new
            {
                applicationId = application.Id,
                applicationNumber = application.ApplicationNumber
            }
        });

        return Ok(ApiResponse<object>.Ok(new object(), "Application deleted successfully."));
    }
}
