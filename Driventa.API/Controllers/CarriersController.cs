using Driventa.API.Hubs;
using Driventa.Application.DTOs.Carriers;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Drivers;
using Driventa.Application.DTOs.Documents;
using Driventa.Application.DTOs.Loads;
using Driventa.Application.DTOs.Trucks;
using Driventa.Application.Interfaces;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarriersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<DashboardHub> _dashboardHub;

    public CarriersController(
        AppDbContext context,
        INotificationService notificationService,
        IHubContext<DashboardHub> dashboardHub)
    {
        _context = context;
        _notificationService = notificationService;
        _dashboardHub = dashboardHub;
    }

    [HttpGet]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CarrierResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] CarrierStatus? status = null)
    {
        var query = _context.Carriers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.CompanyName.Contains(search) ||
                c.ContactName.Contains(search) ||
                c.Email.Contains(search));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CarrierResponse
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                ContactName = c.ContactName,
                Email = c.Email,
                Phone = c.Phone,
                McNumber = c.McNumber,
                DotNumber = c.DotNumber,
                AddressLine1 = c.AddressLine1,
                City = c.City,
                State = c.State,
                ZipCode = c.ZipCode,
                Status = c.Status,
                AssignedDispatcherId = c.AssignedDispatcherId,
                PreferredLanes = c.PreferredLanes,
                Notes = c.Notes,
                ApplicationId = c.ApplicationId,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<CarrierResponse>>.Ok(
            new PaginatedResponse<CarrierResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Policy = "carriers.create")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> Create([FromBody] CreateCarrierRequest request)
    {
        var carrier = new Carrier
        {
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            McNumber = request.McNumber,
            DotNumber = request.DotNumber,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            PreferredLanes = request.PreferredLanes,
            Notes = request.Notes,
            ApplicationId = request.ApplicationId,
            Status = CarrierStatus.Lead
        };

        _context.Carriers.Add(carrier);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Carrier",
            EntityId = carrier.Id,
            Description = $"Carrier {carrier.CompanyName} created"
        });

        await _context.SaveChangesAsync();

        // --- Notify admins of new carrier ---
        var adminRoles = new[] { "SuperAdmin", "Admin", "DispatchManager" };
        foreach (var roleName in adminRoles)
        {
            var usersInRole = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r.Name })
                .Where(x => x.Name == roleName)
                .Select(x => x.u.Id)
                .ToListAsync();

            foreach (var userId in usersInRole)
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    NotificationType.CarrierCreated,
                    "New Carrier",
                    $"{carrier.CompanyName} has been added as a new carrier.",
                    "Carrier",
                    carrier.Id);
            }
        }

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Carrier",
            action = "Created",
            entity = new
            {
                carrierId = carrier.Id,
                companyName = carrier.CompanyName,
                contactName = carrier.ContactName,
                status = carrier.Status.ToString()
            }
        });

        var response = MapToResponse(carrier);
        return Ok(ApiResponse<CarrierResponse>.Ok(response, "Carrier created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> GetById(Guid id)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<CarrierResponse>.Fail("Carrier not found."));

        return Ok(ApiResponse<CarrierResponse>.Ok(MapToResponse(carrier)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "carriers.edit")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> Update(
        Guid id,
        [FromBody] UpdateCarrierRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<CarrierResponse>.Fail("Carrier not found."));

        if (request.CompanyName != null) carrier.CompanyName = request.CompanyName;
        if (request.ContactName != null) carrier.ContactName = request.ContactName;
        if (request.Email != null) carrier.Email = request.Email;
        if (request.Phone != null) carrier.Phone = request.Phone;
        if (request.McNumber != null) carrier.McNumber = request.McNumber;
        if (request.DotNumber != null) carrier.DotNumber = request.DotNumber;
        if (request.AddressLine1 != null) carrier.AddressLine1 = request.AddressLine1;
        if (request.AddressLine2 != null) carrier.AddressLine2 = request.AddressLine2;
        if (request.City != null) carrier.City = request.City;
        if (request.State != null) carrier.State = request.State;
        if (request.ZipCode != null) carrier.ZipCode = request.ZipCode;
        if (request.Status.HasValue) carrier.Status = request.Status.Value;
        if (request.PreferredLanes != null) carrier.PreferredLanes = request.PreferredLanes;
        if (request.Notes != null) carrier.Notes = request.Notes;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Update",
            EntityType = "Carrier",
            EntityId = id,
            Description = $"Carrier {carrier.CompanyName} updated"
        });

        await _context.SaveChangesAsync();

        // Broadcast update
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Carrier",
            action = "Updated",
            entity = new
            {
                carrierId = carrier.Id,
                companyName = carrier.CompanyName,
                status = carrier.Status.ToString()
            }
        });

        return Ok(ApiResponse<CarrierResponse>.Ok(MapToResponse(carrier), "Carrier updated successfully."));
    }

    [HttpPost("{id:guid}/assign-dispatcher")]
    [Authorize(Policy = "carriers.edit")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> AssignDispatcher(
        Guid id,
        [FromBody] AssignDispatcherRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<CarrierResponse>.Fail("Carrier not found."));

        carrier.AssignedDispatcherId = request.DispatcherId;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "AssignDispatcher",
            EntityType = "Carrier",
            EntityId = id,
            Description = $"Dispatcher {request.DispatcherId} assigned to carrier {carrier.CompanyName}"
        });

        await _context.SaveChangesAsync();

        // --- Notify the assigned dispatcher ---
        await _notificationService.CreateNotificationAsync(
            request.DispatcherId,
            NotificationType.DispatcherAssigned,
            "Carrier Assigned",
            $"{carrier.CompanyName} has been assigned to you.",
            "Carrier",
            carrier.Id);

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Carrier",
            action = "DispatcherAssigned",
            entity = new
            {
                carrierId = carrier.Id,
                companyName = carrier.CompanyName,
                dispatcherId = request.DispatcherId
            }
        });

        return Ok(ApiResponse<CarrierResponse>.Ok(MapToResponse(carrier), "Dispatcher assigned successfully."));
    }

    [HttpGet("{id:guid}/loads")]
    [Authorize(Policy = "loads.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<Application.DTOs.Loads.LoadResponse>>>> GetLoads(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<PaginatedResponse<Application.DTOs.Loads.LoadResponse>>.Fail("Carrier not found."));

        var query = _context.Loads
            .Where(l => l.CarrierId == id && !l.IsDeleted);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new Application.DTOs.Loads.LoadResponse
            {
                Id = l.Id,
                LoadNumber = l.LoadNumber,
                CarrierId = l.CarrierId,
                CarrierName = l.Carrier.CompanyName,
                TruckId = l.TruckId,
                TruckNumber = l.Truck != null ? l.Truck.TruckNumber : null,
                DriverId = l.DriverId,
                DriverName = l.Driver != null ? l.Driver.FirstName + " " + l.Driver.LastName : null,
                BrokerId = l.BrokerId,
                BrokerName = l.Broker != null ? l.Broker.CompanyName : null,
                DispatcherId = l.DispatcherId,
                EquipmentType = l.EquipmentType,
                PickupCity = l.PickupCity,
                PickupState = l.PickupState,
                PickupDateTime = l.PickupDateTime,
                DeliveryCity = l.DeliveryCity,
                DeliveryState = l.DeliveryState,
                DeliveryDateTime = l.DeliveryDateTime,
                Rate = l.Rate,
                Miles = l.Miles,
                RatePerMile = l.RatePerMile,
                DispatchFeeAmount = l.DispatchFeeAmount,
                CarrierNetAmount = l.CarrierNetAmount,
                Status = l.Status,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<Application.DTOs.Loads.LoadResponse>>.Ok(
            new PaginatedResponse<Application.DTOs.Loads.LoadResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("{id:guid}/trucks")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TruckResponse>>>> GetTrucks(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<PaginatedResponse<TruckResponse>>.Fail("Carrier not found."));

        var query = _context.Trucks
            .Where(t => t.CarrierId == id && !t.IsDeleted);

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

    [HttpGet("{id:guid}/drivers")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverResponse>>>> GetDrivers(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<PaginatedResponse<DriverResponse>>.Fail("Carrier not found."));

        var query = _context.Drivers
            .Where(d => d.CarrierId == id && !d.IsDeleted);

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

    [HttpGet("{id:guid}/documents")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DocumentResponse>>>> GetDocuments(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<PaginatedResponse<DocumentResponse>>.Fail("Carrier not found."));

        var query = _context.Documents
            .Where(d => d.CarrierId == id && !d.IsDeleted);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentResponse
            {
                Id = d.Id,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                DocumentType = d.DocumentType,
                CarrierId = d.CarrierId,
                LoadId = d.LoadId,
                DriverId = d.DriverId,
                UploadedByUserId = d.UploadedByUserId,
                CreatedAt = d.CreatedAt,
                ExpiresAt = d.ExpiresAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<DocumentResponse>>.Ok(
            new PaginatedResponse<DocumentResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    private static CarrierResponse MapToResponse(Carrier carrier)
    {
        return new CarrierResponse
        {
            Id = carrier.Id,
            CompanyName = carrier.CompanyName,
            ContactName = carrier.ContactName,
            Email = carrier.Email,
            Phone = carrier.Phone,
            McNumber = carrier.McNumber,
            DotNumber = carrier.DotNumber,
            AddressLine1 = carrier.AddressLine1,
            AddressLine2 = carrier.AddressLine2,
            City = carrier.City,
            State = carrier.State,
            ZipCode = carrier.ZipCode,
            Status = carrier.Status,
            AssignedDispatcherId = carrier.AssignedDispatcherId,
            PreferredLanes = carrier.PreferredLanes,
            Notes = carrier.Notes,
            ApplicationId = carrier.ApplicationId,
            CreatedAt = carrier.CreatedAt
        };
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<List<CarrierNote>>>> GetNotes(Guid id)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<List<CarrierNote>>.Fail("Carrier not found."));

        var notes = await _context.CarrierNotes
            .Where(n => n.CarrierId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<CarrierNote>>.Ok(notes));
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = "carriers.view")]
    public async Task<ActionResult<ApiResponse<CarrierNote>>> AddNote(
        Guid id,
        [FromBody] Driventa.Application.DTOs.Applications.ApplicationNoteRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<CarrierNote>.Fail("Carrier not found."));

        var note = new CarrierNote
        {
            CarrierId = id,
            Content = request.Content
        };

        _context.CarrierNotes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<CarrierNote>.Ok(note, "Note added successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "carriers.edit")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (carrier == null)
            return NotFound(ApiResponse<object>.Fail("Carrier not found."));

        carrier.IsDeleted = true;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Delete",
            EntityType = "Carrier",
            EntityId = id,
            Description = $"Carrier {carrier.CompanyName} deleted"
        });

        await _context.SaveChangesAsync();

        // Broadcast deletion
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Carrier",
            action = "Deleted",
            entity = new
            {
                carrierId = carrier.Id,
                companyName = carrier.CompanyName
            }
        });

        return Ok(ApiResponse<object>.Ok(new object(), "Carrier deleted successfully."));
    }
}
