using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Trucks;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrucksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrucksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TruckResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? carrierId = null,
        [FromQuery] TruckStatus? status = null)
    {
        var query = _context.Trucks
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.TruckNumber.Contains(search) ||
                (t.Make != null && t.Make.Contains(search)) ||
                (t.Model != null && t.Model.Contains(search)));

        if (carrierId.HasValue)
            query = query.Where(t => t.CarrierId == carrierId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TruckResponse
            {
                Id = t.Id,
                CarrierId = t.CarrierId,
                TruckNumber = t.TruckNumber,
                EquipmentType = t.EquipmentType,
                Make = t.Make,
                Model = t.Model,
                Year = t.Year,
                LicensePlate = t.LicensePlate,
                LicenseState = t.LicenseState,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<TruckResponse>>.Ok(
            new PaginatedResponse<TruckResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,DispatchManager")]
    public async Task<ActionResult<ApiResponse<TruckResponse>>> Create([FromBody] CreateTruckRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == request.CarrierId && !c.IsDeleted);

        if (carrier == null)
            return BadRequest(ApiResponse<TruckResponse>.Fail("Carrier not found."));

        var truck = new Truck
        {
            CarrierId = request.CarrierId,
            TruckNumber = request.TruckNumber,
            EquipmentType = request.EquipmentType,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            LicensePlate = request.LicensePlate,
            LicenseState = request.LicenseState,
            Status = TruckStatus.Available
        };

        _context.Trucks.Add(truck);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Truck",
            EntityId = truck.Id,
            Description = $"Truck {truck.TruckNumber} created for carrier {carrier.CompanyName}"
        });

        await _context.SaveChangesAsync();

        var response = MapToResponse(truck);
        return Ok(ApiResponse<TruckResponse>.Ok(response, "Truck created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TruckResponse>>> GetById(Guid id)
    {
        var truck = await _context.Trucks
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (truck == null)
            return NotFound(ApiResponse<TruckResponse>.Fail("Truck not found."));

        return Ok(ApiResponse<TruckResponse>.Ok(MapToResponse(truck)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TruckResponse>>> Update(
        Guid id,
        [FromBody] UpdateTruckRequest request)
    {
        var truck = await _context.Trucks
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (truck == null)
            return NotFound(ApiResponse<TruckResponse>.Fail("Truck not found."));

        if (request.TruckNumber != null) truck.TruckNumber = request.TruckNumber;
        if (request.EquipmentType.HasValue) truck.EquipmentType = request.EquipmentType.Value;
        if (request.Make != null) truck.Make = request.Make;
        if (request.Model != null) truck.Model = request.Model;
        if (request.Year.HasValue) truck.Year = request.Year;
        if (request.LicensePlate != null) truck.LicensePlate = request.LicensePlate;
        if (request.LicenseState != null) truck.LicenseState = request.LicenseState;
        if (request.Status.HasValue) truck.Status = request.Status.Value;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Update",
            EntityType = "Truck",
            EntityId = id,
            Description = $"Truck {truck.TruckNumber} updated"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<TruckResponse>.Ok(MapToResponse(truck), "Truck updated successfully."));
    }

    private static TruckResponse MapToResponse(Truck truck)
    {
        return new TruckResponse
        {
            Id = truck.Id,
            CarrierId = truck.CarrierId,
            TruckNumber = truck.TruckNumber,
            EquipmentType = truck.EquipmentType,
            Make = truck.Make,
            Model = truck.Model,
            Year = truck.Year,
            LicensePlate = truck.LicensePlate,
            LicenseState = truck.LicenseState,
            Status = truck.Status,
            CreatedAt = truck.CreatedAt
        };
    }
}
