using System.Security.Claims;
using Driventa.API.Hubs;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Documents;
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
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentsController(
        AppDbContext context,
        IWebHostEnvironment env,
        INotificationService notificationService,
        IHubContext<DashboardHub> dashboardHub,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _env = env;
        _notificationService = notificationService;
        _dashboardHub = dashboardHub;
        _userManager = userManager;
    }

    [HttpPost("upload")]
    [Authorize(Policy = "carriers.view")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> Upload(
        IFormFile file,
        [FromQuery] DocumentType documentType,
        [FromQuery] Guid? carrierId = null,
        [FromQuery] Guid? loadId = null,
        [FromQuery] Guid? driverId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<DocumentResponse>.Fail("No file uploaded."));

        var uploadsPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, storedFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var document = new Document
        {
            FileName = file.FileName,
            StoredFileName = storedFileName,
            FileUrl = $"/uploads/{storedFileName}",
            ContentType = file.ContentType,
            FileSize = file.Length,
            DocumentType = documentType,
            CarrierId = carrierId,
            LoadId = loadId,
            DriverId = driverId,
            UploadedByUserId = userId != null ? Guid.Parse(userId) : null
        };

        _context.Documents.Add(document);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Upload",
            EntityType = "Document",
            EntityId = document.Id,
            Description = $"Document {file.FileName} uploaded ({documentType})"
        });

        await _context.SaveChangesAsync();

        // --- Notify the carrier's assigned dispatcher ---
        Guid? targetDispatcherId = null;
        string entityName = "";

        if (carrierId.HasValue)
        {
            var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId.Value);
            if (carrier != null)
            {
                targetDispatcherId = carrier.AssignedDispatcherId;
                entityName = carrier.CompanyName;
            }
        }
        else if (loadId.HasValue)
        {
            var load = await _context.Loads.Include(l => l.Carrier).FirstOrDefaultAsync(l => l.Id == loadId.Value);
            if (load?.Carrier != null)
            {
                targetDispatcherId = load.Carrier.AssignedDispatcherId;
                entityName = $"Load {load.LoadNumber}";
            }
        }
        else if (driverId.HasValue)
        {
            var driver = await _context.Drivers.Include(d => d.Carrier).FirstOrDefaultAsync(d => d.Id == driverId.Value);
            if (driver?.Carrier != null)
            {
                targetDispatcherId = driver.Carrier.AssignedDispatcherId;
                entityName = $"{driver.FirstName} {driver.LastName}";
            }
        }

        if (targetDispatcherId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                targetDispatcherId.Value,
                NotificationType.DocumentUploaded,
                "Document Uploaded",
                $"{file.FileName} ({documentType}) uploaded for {entityName}.",
                "Document",
                document.Id);
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
                        NotificationType.DocumentUploaded,
                        "Document Uploaded",
                        $"{file.FileName} ({documentType}) uploaded for {entityName}.",
                        "Document",
                        document.Id);
                }
            }
        }

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Document",
            action = "Uploaded",
            entity = new
            {
                documentId = document.Id,
                fileName = document.FileName,
                documentType = document.DocumentType.ToString(),
                carrierId = document.CarrierId,
                loadId = document.LoadId,
                driverId = document.DriverId
            }
        });

        var response = MapToResponse(document);
        return Ok(ApiResponse<DocumentResponse>.Ok(response, "Document uploaded successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> GetById(Guid id)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (document == null)
            return NotFound(ApiResponse<DocumentResponse>.Fail("Document not found."));

        return Ok(ApiResponse<DocumentResponse>.Ok(MapToResponse(document)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "billing.manage")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (document == null)
            return NotFound(ApiResponse<object>.Fail("Document not found."));

        var filePath = Path.Combine(
            _env.WebRootPath ?? "wwwroot",
            "uploads",
            document.StoredFileName);

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        document.IsDeleted = true;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Delete",
            EntityType = "Document",
            EntityId = id,
            Description = $"Document {document.FileName} deleted"
        });

        await _context.SaveChangesAsync();

        // Broadcast deletion
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Document",
            action = "Deleted",
            entity = new
            {
                documentId = document.Id,
                fileName = document.FileName
            }
        });

        return Ok(ApiResponse<object>.Ok(new { }, "Document deleted successfully."));
    }

    private static DocumentResponse MapToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            FileUrl = document.FileUrl,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            DocumentType = document.DocumentType,
            CarrierId = document.CarrierId,
            LoadId = document.LoadId,
            DriverId = document.DriverId,
            UploadedByUserId = document.UploadedByUserId,
            CreatedAt = document.CreatedAt,
            ExpiresAt = document.ExpiresAt
        };
    }
}
