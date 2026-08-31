using Driventa.API.Hubs;
using Driventa.Application.DTOs.Applications;
using Driventa.Application.DTOs.Common;
using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/applications")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<ApplicationsHub> _applicationsHub;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly UserManager<ApplicationUser> _userManager;

    public PublicApplicationsController(
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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> SubmitApplication(
        [FromBody] PublicApplicationRequest request)
    {
        var applicationNumber = GenerateApplicationNumber();

        var application = new Domain.Entities.Application
        {
            ApplicationNumber = applicationNumber,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            EquipmentType = request.EquipmentType,
            TruckCount = request.TruckCount,
            McNumber = request.McNumber,
            DotNumber = request.DotNumber,
            PreferredLanes = request.PreferredLanes,
            AdditionalDetails = request.AdditionalDetails,
            Status = ApplicationStatus.New,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "NewApplication",
            EntityType = "Application",
            EntityId = application.Id,
            Description = $"New application submitted by {request.FullName} ({request.CompanyName})"
        });

        await _context.SaveChangesAsync();

        // --- Real-time notifications ---
        var applicationData = new
        {
            applicationId = application.Id,
            applicationNumber = application.ApplicationNumber,
            companyName = application.CompanyName,
            fullName = application.FullName,
            email = application.Email,
            phone = application.Phone,
            equipmentType = application.EquipmentType.ToString(),
            truckCount = application.TruckCount,
            status = application.Status.ToString(),
            submittedAt = application.SubmittedAt
        };

        // Broadcast to all connected admins via ApplicationsHub
        await _applicationsHub.Clients.Group("admins").SendAsync("ApplicationCreated", applicationData);

        // Broadcast to dashboard hub
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Application",
            action = "Created",
            entity = applicationData
        });

        // Persist notification to all admin/dispatch users
        var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
        foreach (var roleName in adminRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    NotificationType.NewApplication,
                    "New Application",
                    $"{request.CompanyName} ({request.FullName}) submitted a new application.",
                    "Application",
                    application.Id);
            }
        }

        return Ok(ApiResponse<object>.Ok(
            new { application.Id, application.ApplicationNumber },
            "Application submitted successfully."));
    }

    private static string GenerateApplicationNumber()
    {
        var now = DateTimeOffset.UtcNow;
        var unique = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"APP-{now:yyMMdd}-{unique}";
    }
}
