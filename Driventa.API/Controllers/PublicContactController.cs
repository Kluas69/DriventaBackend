using Driventa.API.Hubs;
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

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/contact")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicContactController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly UserManager<ApplicationUser> _userManager;

    public PublicContactController(
        AppDbContext context,
        INotificationService notificationService,
        IHubContext<DashboardHub> dashboardHub,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _dashboardHub = dashboardHub;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> SubmitContact(
        [FromBody] PublicContactRequest request)
    {
        var contactMessage = new ActivityLog
        {
            Action = "ContactForm",
            EntityType = "ContactMessage",
            Description = $"Contact from {request.Name} ({request.Email}): {request.Subject}",
            OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                request.Name,
                request.Email,
                request.Phone,
                request.Subject,
                request.Message
            })
        };

        _context.ActivityLogs.Add(contactMessage);
        await _context.SaveChangesAsync();

        // --- Notify all admin/dispatch users of new contact form submission ---
        var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager", "Dispatcher" };
        foreach (var roleName in adminRoles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    NotificationType.NewMessage,
                    "New Contact Form",
                    $"{request.Name} ({request.Email}) submitted a contact form: {request.Subject}",
                    "ContactMessage",
                    contactMessage.Id);
            }
        }

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "ContactMessage",
            action = "Created",
            entity = new
            {
                contactId = contactMessage.Id,
                name = request.Name,
                email = request.Email,
                phone = request.Phone,
                subject = request.Subject,
                message = request.Message,
                timestamp = DateTimeOffset.UtcNow
            }
        });

        return Ok(ApiResponse<object>.Ok(
            new { },
            "Your message has been received. We will get back to you shortly."));
    }
}

public class PublicContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
