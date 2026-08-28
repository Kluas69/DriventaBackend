using Driventa.Application.DTOs.Common;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/public/contact")]
[EnableRateLimiting("PublicEndpoints")]
public class PublicContactController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicContactController(AppDbContext context)
    {
        _context = context;
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
