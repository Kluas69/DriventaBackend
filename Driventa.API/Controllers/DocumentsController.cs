using System.Security.Claims;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Documents;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
