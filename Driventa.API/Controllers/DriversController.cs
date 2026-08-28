using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Drivers;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriversController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? carrierId = null,
        [FromQuery] DriverStatus? status = null)
    {
        var query = _context.Drivers
            .Where(d => !d.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d =>
                d.FirstName.Contains(search) ||
                d.LastName.Contains(search) ||
                (d.Email != null && d.Email.Contains(search)));

        if (carrierId.HasValue)
            query = query.Where(d => d.CarrierId == carrierId.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DriverResponse
            {
                Id = d.Id,
                CarrierId = d.CarrierId,
                TruckId = d.TruckId,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                Phone = d.Phone,
                LicenseNumber = d.LicenseNumber,
                LicenseState = d.LicenseState,
                Status = d.Status,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<DriverResponse>>.Ok(
            new PaginatedResponse<DriverResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,DispatchManager")]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Create([FromBody] CreateDriverRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == request.CarrierId && !c.IsDeleted);

        if (carrier == null)
            return BadRequest(ApiResponse<DriverResponse>.Fail("Carrier not found."));

        if (request.TruckId.HasValue)
        {
            var truck = await _context.Trucks
                .FirstOrDefaultAsync(t => t.Id == request.TruckId.Value && !t.IsDeleted);

            if (truck == null)
                return BadRequest(ApiResponse<DriverResponse>.Fail("Truck not found."));
        }

        var driver = new Driver
        {
            CarrierId = request.CarrierId,
            TruckId = request.TruckId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            LicenseNumber = request.LicenseNumber,
            LicenseState = request.LicenseState,
            Status = DriverStatus.Available
        };

        _context.Drivers.Add(driver);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Driver",
            EntityId = driver.Id,
            Description = $"Driver {driver.FirstName} {driver.LastName} created for carrier {carrier.CompanyName}"
        });

        await _context.SaveChangesAsync();

        var response = MapToResponse(driver);
        return Ok(ApiResponse<DriverResponse>.Ok(response, "Driver created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> GetById(Guid id)
    {
        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (driver == null)
            return NotFound(ApiResponse<DriverResponse>.Fail("Driver not found."));

        return Ok(ApiResponse<DriverResponse>.Ok(MapToResponse(driver)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Update(
        Guid id,
        [FromBody] UpdateDriverRequest request)
    {
        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (driver == null)
            return NotFound(ApiResponse<DriverResponse>.Fail("Driver not found."));

        if (request.TruckId.HasValue) driver.TruckId = request.TruckId;
        if (request.FirstName != null) driver.FirstName = request.FirstName;
        if (request.LastName != null) driver.LastName = request.LastName;
        if (request.Email != null) driver.Email = request.Email;
        if (request.Phone != null) driver.Phone = request.Phone;
        if (request.LicenseNumber != null) driver.LicenseNumber = request.LicenseNumber;
        if (request.LicenseState != null) driver.LicenseState = request.LicenseState;
        if (request.Status.HasValue) driver.Status = request.Status.Value;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Update",
            EntityType = "Driver",
            EntityId = id,
            Description = $"Driver {driver.FirstName} {driver.LastName} updated"
        });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<DriverResponse>.Ok(MapToResponse(driver), "Driver updated successfully."));
    }

    private static DriverResponse MapToResponse(Driver driver)
    {
        return new DriverResponse
        {
            Id = driver.Id,
            CarrierId = driver.CarrierId,
            TruckId = driver.TruckId,
            FirstName = driver.FirstName,
            LastName = driver.LastName,
            Email = driver.Email,
            Phone = driver.Phone,
            LicenseNumber = driver.LicenseNumber,
            LicenseState = driver.LicenseState,
            Status = driver.Status,
            CreatedAt = driver.CreatedAt
        };
    }
}
