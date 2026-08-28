using Driventa.Application.DTOs.Brokers;
using Driventa.Application.DTOs.Common;
using Driventa.Domain.Entities;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrokersController : ControllerBase
{
    private readonly AppDbContext _context;

    public BrokersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<BrokerResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _context.Brokers
            .Where(b => !b.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b =>
                b.CompanyName.Contains(search) ||
                b.ContactName.Contains(search) ||
                b.Email.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BrokerResponse
            {
                Id = b.Id,
                CompanyName = b.CompanyName,
                ContactName = b.ContactName,
                Email = b.Email,
                Phone = b.Phone,
                McNumber = b.McNumber,
                Address = b.Address,
                InternalRating = b.InternalRating,
                PaymentNotes = b.PaymentNotes,
                GeneralNotes = b.GeneralNotes,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<BrokerResponse>>.Ok(
            new PaginatedResponse<BrokerResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,DispatchManager")]
    public async Task<ActionResult<ApiResponse<BrokerResponse>>> Create([FromBody] CreateBrokerRequest request)
    {
        var broker = new Broker
        {
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            McNumber = request.McNumber,
            Address = request.Address,
            InternalRating = request.InternalRating,
            PaymentNotes = request.PaymentNotes,
            GeneralNotes = request.GeneralNotes,
            IsActive = true
        };

        _context.Brokers.Add(broker);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Broker",
            EntityId = broker.Id,
            Description = $"Broker {broker.CompanyName} created"
        });

        await _context.SaveChangesAsync();

        var response = MapToResponse(broker);
        return Ok(ApiResponse<BrokerResponse>.Ok(response, "Broker created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BrokerResponse>>> GetById(Guid id)
    {
        var broker = await _context.Brokers
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (broker == null)
            return NotFound(ApiResponse<BrokerResponse>.Fail("Broker not found."));

        return Ok(ApiResponse<BrokerResponse>.Ok(MapToResponse(broker)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BrokerResponse>>> Update(
        Guid id,
        [FromBody] UpdateBrokerRequest request)
    {
        var broker = await _context.Brokers
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (broker == null)
            return NotFound(ApiResponse<BrokerResponse>.Fail("Broker not found."));

        if (request.CompanyName != null) broker.CompanyName = request.CompanyName;
        if (request.ContactName != null) broker.ContactName = request.ContactName;
        if (request.Email != null) broker.Email = request.Email;
        if (request.Phone != null) broker.Phone = request.Phone;
        if (request.McNumber != null) broker.McNumber = request.McNumber;
        if (request.Address != null) broker.Address = request.Address;
        if (request.InternalRating.HasValue) broker.InternalRating = request.InternalRating;
        if (request.PaymentNotes != null) broker.PaymentNotes = request.PaymentNotes;
        if (request.GeneralNotes != null) broker.GeneralNotes = request.GeneralNotes;
        if (request.IsActive.HasValue) broker.IsActive = request.IsActive.Value;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Update",
            EntityType = "Broker",
            EntityId = id,
            Description = $"Broker {broker.CompanyName} updated"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<BrokerResponse>.Ok(MapToResponse(broker), "Broker updated successfully."));
    }

    private static BrokerResponse MapToResponse(Broker broker)
    {
        return new BrokerResponse
        {
            Id = broker.Id,
            CompanyName = broker.CompanyName,
            ContactName = broker.ContactName,
            Email = broker.Email,
            Phone = broker.Phone,
            McNumber = broker.McNumber,
            Address = broker.Address,
            InternalRating = broker.InternalRating,
            PaymentNotes = broker.PaymentNotes,
            GeneralNotes = broker.GeneralNotes,
            IsActive = broker.IsActive,
            CreatedAt = broker.CreatedAt
        };
    }
}
