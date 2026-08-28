using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Dashboard;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var newApplications = await _context.Applications
            .CountAsync(a => !a.IsDeleted && a.Status == ApplicationStatus.New);

        var applicationsInReview = await _context.Applications
            .CountAsync(a => !a.IsDeleted && a.Status == ApplicationStatus.Reviewing);

        var activeCarriers = await _context.Carriers
            .CountAsync(c => !c.IsDeleted && c.Status == Domain.Enums.CarrierStatus.Active);

        var activeTrucks = await _context.Trucks
            .CountAsync(t => !t.IsDeleted && t.Status == Domain.Enums.TruckStatus.Available);

        var activeLoads = await _context.Loads
            .CountAsync(l => !l.IsDeleted &&
                l.Status != LoadStatus.Completed &&
                l.Status != LoadStatus.Cancelled);

        var loadsInTransit = await _context.Loads
            .CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.InTransit);

        var completedLoadsThisMonth = await _context.Loads
            .CountAsync(l => !l.IsDeleted &&
                l.Status == LoadStatus.Completed &&
                l.CompletedAt >= startOfMonth);

        var dispatchRevenueThisMonth = await _context.Loads
            .Where(l => !l.IsDeleted &&
                l.Status == LoadStatus.Completed &&
                l.CompletedAt >= startOfMonth)
            .SumAsync(l => l.DispatchFeeAmount ?? 0);

        var response = new DashboardSummaryResponse
        {
            NewApplications = newApplications,
            ApplicationsInReview = applicationsInReview,
            ActiveCarriers = activeCarriers,
            ActiveTrucks = activeTrucks,
            ActiveLoads = activeLoads,
            LoadsInTransit = loadsInTransit,
            CompletedLoadsThisMonth = completedLoadsThisMonth,
            DispatchRevenueThisMonth = Math.Round(dispatchRevenueThisMonth, 2)
        };

        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(response));
    }

    [HttpGet("recent-applications")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<Domain.Entities.Application>>>> GetRecentApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Applications
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.SubmittedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<Domain.Entities.Application>>.Ok(
            new PaginatedResponse<Domain.Entities.Application>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("recent-activity")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<RecentActivityResponse>>>> GetRecentActivity(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.ActivityLogs
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new RecentActivityResponse
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<RecentActivityResponse>>.Ok(
            new PaginatedResponse<RecentActivityResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpGet("load-status-summary")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<LoadStatusSummaryResponse>>> GetLoadStatusSummary()
    {
        var response = new LoadStatusSummaryResponse
        {
            Available = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Available),
            Negotiating = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Negotiating),
            Booked = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Booked),
            Dispatched = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Dispatched),
            PickedUp = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.PickedUp),
            InTransit = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.InTransit),
            Delivered = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Delivered),
            Completed = await _context.Loads.CountAsync(l => !l.IsDeleted && l.Status == LoadStatus.Completed)
        };

        return Ok(ApiResponse<LoadStatusSummaryResponse>.Ok(response));
    }

    [HttpGet("revenue-summary")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RevenueSummaryResponse>>> GetRevenueSummary()
    {
        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var startOfYear = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var revenueThisMonth = await _context.Loads
            .Where(l => !l.IsDeleted && l.Status == LoadStatus.Completed && l.CompletedAt >= startOfMonth)
            .SumAsync(l => l.DispatchFeeAmount ?? 0);

        var revenueThisYear = await _context.Loads
            .Where(l => !l.IsDeleted && l.Status == LoadStatus.Completed && l.CompletedAt >= startOfYear)
            .SumAsync(l => l.DispatchFeeAmount ?? 0);

        var totalRevenue = await _context.Loads
            .Where(l => !l.IsDeleted && l.Status == LoadStatus.Completed)
            .SumAsync(l => l.DispatchFeeAmount ?? 0);

        var pendingPayments = await _context.Invoices
            .Where(i => !i.IsDeleted && (i.Status == Domain.Enums.InvoiceStatus.Sent || i.Status == Domain.Enums.InvoiceStatus.Overdue))
            .SumAsync(i => i.TotalAmount);

        var response = new RevenueSummaryResponse
        {
            RevenueThisMonth = Math.Round(revenueThisMonth, 2),
            RevenueThisYear = Math.Round(revenueThisYear, 2),
            TotalRevenue = Math.Round(totalRevenue, 2),
            PendingPayments = Math.Round(pendingPayments, 2)
        };

        return Ok(ApiResponse<RevenueSummaryResponse>.Ok(response));
    }
}
