using System.Security.Claims;
using Driventa.API.Hubs;
using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Loads;
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
public class LoadsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<DashboardHub> _dashboardHub;

    public LoadsController(
        AppDbContext context,
        INotificationService notificationService,
        IHubContext<DashboardHub> dashboardHub)
    {
        _context = context;
        _notificationService = notificationService;
        _dashboardHub = dashboardHub;
    }

    [HttpGet]
    [Authorize(Policy = "loads.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LoadResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] LoadStatus? status = null,
        [FromQuery] Guid? carrierId = null)
    {
        var query = _context.Loads
            .Include(l => l.Carrier)
            .Include(l => l.Truck)
            .Include(l => l.Driver)
            .Include(l => l.Broker)
            .Where(l => !l.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                l.LoadNumber.Contains(search) ||
                l.PickupCity.Contains(search) ||
                l.DeliveryCity.Contains(search));

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (carrierId.HasValue)
            query = query.Where(l => l.CarrierId == carrierId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LoadResponse
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

        return Ok(ApiResponse<PaginatedResponse<LoadResponse>>.Ok(
            new PaginatedResponse<LoadResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost]
    [Authorize(Policy = "loads.create")]
    public async Task<ActionResult<ApiResponse<LoadResponse>>> Create([FromBody] CreateLoadRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == request.CarrierId && !c.IsDeleted);

        if (carrier == null)
            return BadRequest(ApiResponse<LoadResponse>.Fail("Carrier not found."));

        var loadNumber = GenerateLoadNumber();

        var load = new Load
        {
            LoadNumber = loadNumber,
            CarrierId = request.CarrierId,
            TruckId = request.TruckId,
            DriverId = request.DriverId,
            BrokerId = request.BrokerId,
            EquipmentType = request.EquipmentType,
            PickupCity = request.PickupCity,
            PickupState = request.PickupState,
            PickupDateTime = request.PickupDateTime,
            DeliveryCity = request.DeliveryCity,
            DeliveryState = request.DeliveryState,
            DeliveryDateTime = request.DeliveryDateTime,
            Rate = request.Rate,
            Miles = request.Miles,
            DispatchFeeType = request.DispatchFeeType,
            DispatchFeeValue = request.DispatchFeeValue,
            Status = LoadStatus.Available
        };

        // Calculate financial fields
        if (request.Miles.HasValue && request.Miles.Value > 0)
            load.RatePerMile = Math.Round(request.Rate / request.Miles.Value, 2);

        if (request.DispatchFeeType == "Percentage" && request.DispatchFeeValue.HasValue)
            load.DispatchFeeAmount = Math.Round(request.Rate * request.DispatchFeeValue.Value / 100, 2);
        else if (request.DispatchFeeType == "Flat" && request.DispatchFeeValue.HasValue)
            load.DispatchFeeAmount = request.DispatchFeeValue.Value;

        load.CarrierNetAmount = request.Rate - (load.DispatchFeeAmount ?? 0);

        _context.Loads.Add(load);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Load",
            EntityId = load.Id,
            Description = $"Load {load.LoadNumber} created for carrier {carrier.CompanyName}"
        });

        await _context.SaveChangesAsync();

        // Reload with navigation properties for response
        await _context.Entry(load).Reference(l => l.Carrier).LoadAsync();

        // --- Notify carrier's assigned dispatcher ---
        if (carrier.AssignedDispatcherId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                carrier.AssignedDispatcherId.Value,
                NotificationType.LoadCreated,
                "New Load Assigned",
                $"Load {load.LoadNumber} ({load.PickupCity}, {load.PickupState} → {load.DeliveryCity}, {load.DeliveryState}) has been assigned to {carrier.CompanyName}.",
                "Load",
                load.Id);
        }

        // Broadcast to dashboard
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Load",
            action = "Created",
            entity = new
            {
                loadId = load.Id,
                loadNumber = load.LoadNumber,
                carrierName = carrier.CompanyName,
                pickupCity = load.PickupCity,
                pickupState = load.PickupState,
                deliveryCity = load.DeliveryCity,
                deliveryState = load.DeliveryState,
                rate = load.Rate,
                status = load.Status.ToString()
            }
        });

        var response = MapToResponse(load);
        return Ok(ApiResponse<LoadResponse>.Ok(response, "Load created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "loads.view")]
    public async Task<ActionResult<ApiResponse<LoadResponse>>> GetById(Guid id)
    {
        var load = await _context.Loads
            .Include(l => l.Carrier)
            .Include(l => l.Truck)
            .Include(l => l.Driver)
            .Include(l => l.Broker)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<LoadResponse>.Fail("Load not found."));

        return Ok(ApiResponse<LoadResponse>.Ok(MapToResponse(load)));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "loads.edit")]
    public async Task<ActionResult<ApiResponse<LoadResponse>>> Update(
        Guid id,
        [FromBody] UpdateLoadRequest request)
    {
        var load = await _context.Loads
            .Include(l => l.Carrier)
            .Include(l => l.Truck)
            .Include(l => l.Driver)
            .Include(l => l.Broker)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<LoadResponse>.Fail("Load not found."));

        if (request.TruckId.HasValue) load.TruckId = request.TruckId;
        if (request.DriverId.HasValue) load.DriverId = request.DriverId;
        if (request.BrokerId.HasValue) load.BrokerId = request.BrokerId;
        if (request.PickupCity != null) load.PickupCity = request.PickupCity;
        if (request.PickupState != null) load.PickupState = request.PickupState;
        if (request.PickupDateTime.HasValue) load.PickupDateTime = request.PickupDateTime.Value;
        if (request.DeliveryCity != null) load.DeliveryCity = request.DeliveryCity;
        if (request.DeliveryState != null) load.DeliveryState = request.DeliveryState;
        if (request.DeliveryDateTime.HasValue) load.DeliveryDateTime = request.DeliveryDateTime.Value;
        if (request.Rate.HasValue) load.Rate = request.Rate.Value;
        if (request.Miles.HasValue) load.Miles = request.Miles;
        if (request.DispatchFeeType != null) load.DispatchFeeType = request.DispatchFeeType;
        if (request.DispatchFeeValue.HasValue) load.DispatchFeeValue = request.DispatchFeeValue;

        // Recalculate financials
        if (load.Miles.HasValue && load.Miles.Value > 0)
            load.RatePerMile = Math.Round(load.Rate / load.Miles.Value, 2);

        if (load.DispatchFeeType == "Percentage" && load.DispatchFeeValue.HasValue)
            load.DispatchFeeAmount = Math.Round(load.Rate * load.DispatchFeeValue.Value / 100, 2);
        else if (load.DispatchFeeType == "Flat" && load.DispatchFeeValue.HasValue)
            load.DispatchFeeAmount = load.DispatchFeeValue.Value;

        load.CarrierNetAmount = load.Rate - (load.DispatchFeeAmount ?? 0);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Update",
            EntityType = "Load",
            EntityId = id,
            Description = $"Load {load.LoadNumber} updated"
        });

        await _context.SaveChangesAsync();

        // Broadcast update
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Load",
            action = "Updated",
            entity = new
            {
                loadId = load.Id,
                loadNumber = load.LoadNumber,
                status = load.Status.ToString()
            }
        });

        return Ok(ApiResponse<LoadResponse>.Ok(MapToResponse(load), "Load updated successfully."));
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = "loads.updateStatus")]
    public async Task<ActionResult<ApiResponse<LoadResponse>>> UpdateStatus(
        Guid id,
        [FromBody] LoadStatusUpdateRequest request)
    {
        var load = await _context.Loads
            .Include(l => l.Carrier)
            .Include(l => l.Truck)
            .Include(l => l.Driver)
            .Include(l => l.Broker)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<LoadResponse>.Fail("Load not found."));

        var oldStatus = load.Status;
        load.Status = request.Status;

        if (request.Status == LoadStatus.Booked)
            load.BookedAt = DateTimeOffset.UtcNow;
        else if (request.Status == LoadStatus.PickedUp)
            load.PickedUpAt = DateTimeOffset.UtcNow;
        else if (request.Status == LoadStatus.Delivered)
            load.DeliveredAt = DateTimeOffset.UtcNow;
        else if (request.Status == LoadStatus.Completed)
            load.CompletedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            _context.LoadNotes.Add(new LoadNote
            {
                LoadId = id,
                Content = request.Notes
            });
        }

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "StatusChange",
            EntityType = "Load",
            EntityId = id,
            Description = $"Load status changed to {request.Status}"
        });

        await _context.SaveChangesAsync();

        // --- Notify carrier's assigned dispatcher of status change ---
        if (load.Carrier?.AssignedDispatcherId.HasValue == true)
        {
            await _notificationService.CreateNotificationAsync(
                load.Carrier.AssignedDispatcherId.Value,
                NotificationType.LoadStatusChanged,
                "Load Status Updated",
                $"Load {load.LoadNumber} status changed: {oldStatus} → {request.Status}",
                "Load",
                load.Id);
        }

        // Broadcast status change
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Load",
            action = "StatusChanged",
            entity = new
            {
                loadId = load.Id,
                loadNumber = load.LoadNumber,
                carrierName = load.Carrier?.CompanyName,
                oldStatus = oldStatus.ToString(),
                newStatus = request.Status.ToString(),
                timestamp = DateTimeOffset.UtcNow
            }
        });

        return Ok(ApiResponse<LoadResponse>.Ok(MapToResponse(load), "Load status updated successfully."));
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = "loads.view")]
    public async Task<ActionResult<ApiResponse<LoadNote>>> AddNote(
        Guid id,
        [FromBody] LoadNoteRequest request)
    {
        var load = await _context.Loads
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<LoadNote>.Fail("Load not found."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = new LoadNote
        {
            LoadId = id,
            Content = request.Content,
            CreatedByUserId = userId != null ? Guid.Parse(userId) : null
        };

        _context.LoadNotes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<LoadNote>.Ok(note, "Note added successfully."));
    }

    private static string GenerateLoadNumber()
    {
        var now = DateTimeOffset.UtcNow;
        var unique = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"LD-{now:yyMMdd}-{unique}";
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize(Policy = "loads.view")]
    public async Task<ActionResult<ApiResponse<List<LoadNote>>>> GetNotes(Guid id)
    {
        var load = await _context.Loads
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<List<LoadNote>>.Fail("Load not found."));

        var notes = await _context.LoadNotes
            .Where(n => n.LoadId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<LoadNote>>.Ok(notes));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "loads.edit")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var load = await _context.Loads
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (load == null)
            return NotFound(ApiResponse<object>.Fail("Load not found."));

        load.IsDeleted = true;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Delete",
            EntityType = "Load",
            EntityId = id,
            Description = $"Load {load.LoadNumber} deleted"
        });

        await _context.SaveChangesAsync();

        // Broadcast deletion
        await _dashboardHub.Clients.Group("dashboard-admins").SendAsync("DashboardUpdate", new
        {
            entityType = "Load",
            action = "Deleted",
            entity = new
            {
                loadId = load.Id,
                loadNumber = load.LoadNumber
            }
        });

        return Ok(ApiResponse<object>.Ok(new object(), "Load deleted successfully."));
    }

    private static LoadResponse MapToResponse(Load load)
    {
        return new LoadResponse
        {
            Id = load.Id,
            LoadNumber = load.LoadNumber,
            CarrierId = load.CarrierId,
            CarrierName = load.Carrier?.CompanyName,
            TruckId = load.TruckId,
            TruckNumber = load.Truck?.TruckNumber,
            DriverId = load.DriverId,
            DriverName = load.Driver != null ? $"{load.Driver.FirstName} {load.Driver.LastName}" : null,
            BrokerId = load.BrokerId,
            BrokerName = load.Broker?.CompanyName,
            DispatcherId = load.DispatcherId,
            EquipmentType = load.EquipmentType,
            PickupCity = load.PickupCity,
            PickupState = load.PickupState,
            PickupDateTime = load.PickupDateTime,
            DeliveryCity = load.DeliveryCity,
            DeliveryState = load.DeliveryState,
            DeliveryDateTime = load.DeliveryDateTime,
            Rate = load.Rate,
            Miles = load.Miles,
            RatePerMile = load.RatePerMile,
            DispatchFeeAmount = load.DispatchFeeAmount,
            CarrierNetAmount = load.CarrierNetAmount,
            Status = load.Status,
            CreatedAt = load.CreatedAt
        };
    }
}
