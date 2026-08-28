using System.Security.Claims;
using Driventa.Application.DTOs.Common;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var query = _context.Notifications
            .Where(n => n.UserId == userId.Value)
            .AsQueryable();

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                EntityType = n.EntityType,
                EntityId = n.EntityId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<NotificationResponse>>.Ok(
            new PaginatedResponse<NotificationResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> GetById(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

        if (notification == null)
            return NotFound(ApiResponse<NotificationResponse>.Fail("Notification not found."));

        return Ok(ApiResponse<NotificationResponse>.Ok(new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        }));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead);

        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

        if (notification == null)
            return NotFound(ApiResponse<object>.Fail("Notification not found."));

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { }, "Notification marked as read."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { count = unreadNotifications.Count }, "All notifications marked as read."));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim, out var userId))
            return userId;
        return null;
    }
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Domain.Enums.NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
