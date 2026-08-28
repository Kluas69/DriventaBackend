using Driventa.Application.DTOs.Applications;
using Driventa.Application.DTOs.Common;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/applications")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicApplicationsController(AppDbContext context)
    {
        _context = context;
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
