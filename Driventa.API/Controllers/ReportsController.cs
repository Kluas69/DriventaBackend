using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Reports;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("loads")]
    [Authorize(Policy = "reports.view")]
    public async Task<ActionResult<ApiResponse<LoadReportResponse>>> GetLoadReport()
    {
        var loads = _context.Loads.Where(l => !l.IsDeleted);

        var totalLoads = await loads.CountAsync();
        var activeLoads = await loads.CountAsync(l =>
            l.Status != LoadStatus.Completed && l.Status != LoadStatus.Cancelled);
        var completedLoads = await loads.CountAsync(l => l.Status == LoadStatus.Completed);
        var cancelledLoads = await loads.CountAsync(l => l.Status == LoadStatus.Cancelled);
        var averageRate = totalLoads > 0 ? await loads.AverageAsync(l => l.Rate) : 0;

        var loadsRpmQuery = loads.Where(l => l.RatePerMile.HasValue);
        var hasRpmData = await loadsRpmQuery.AnyAsync();
        var averageRpm = hasRpmData ? await loadsRpmQuery.AverageAsync(l => l.RatePerMile!.Value) : 0;

        var totalRevenue = await loads
            .Where(l => l.Status == LoadStatus.Completed)
            .SumAsync(l => l.Rate);

        var response = new LoadReportResponse
        {
            TotalLoads = totalLoads,
            ActiveLoads = activeLoads,
            CompletedLoads = completedLoads,
            CancelledLoads = cancelledLoads,
            AverageRate = Math.Round(averageRate, 2),
            AverageRPM = Math.Round(averageRpm, 2),
            TotalRevenue = Math.Round(totalRevenue, 2)
        };

        return Ok(ApiResponse<LoadReportResponse>.Ok(response));
    }

    [HttpGet("revenue")]
    [Authorize(Policy = "reports.view")]
    public async Task<ActionResult<ApiResponse<RevenueReportResponse>>> GetRevenueReport()
    {
        var completedLoads = await _context.Loads
            .Where(l => !l.IsDeleted && l.Status == LoadStatus.Completed)
            .ToListAsync();

        var totalRevenue = completedLoads.Sum(l => l.Rate);
        var totalDispatchFees = completedLoads.Sum(l => l.DispatchFeeAmount ?? 0);
        var totalCarrierPayouts = completedLoads.Sum(l => l.CarrierNetAmount ?? 0);
        var averageRevenuePerLoad = completedLoads.Count > 0
            ? totalRevenue / completedLoads.Count
            : 0;

        var monthlyBreakdown = completedLoads
            .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
            .Select(g => new MonthlyRevenue
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = Math.Round(g.Sum(l => l.Rate), 2),
                LoadCount = g.Count()
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        var response = new RevenueReportResponse
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalDispatchFees = Math.Round(totalDispatchFees, 2),
            TotalCarrierPayouts = Math.Round(totalCarrierPayouts, 2),
            AverageRevenuePerLoad = Math.Round(averageRevenuePerLoad, 2),
            MonthlyBreakdown = monthlyBreakdown
        };

        return Ok(ApiResponse<RevenueReportResponse>.Ok(response));
    }

    [HttpGet("carriers")]
    [Authorize(Policy = "reports.view")]
    public async Task<ActionResult<ApiResponse<List<CarrierReportResponse>>>> GetCarrierReport()
    {
        var carriers = await _context.Carriers
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        var carrierIds = carriers.Select(c => c.Id).ToList();

        var loads = await _context.Loads
            .Where(l => carrierIds.Contains(l.CarrierId) && !l.IsDeleted)
            .ToListAsync();

        var report = carriers.Select(c => {
            var carrierLoads = loads.Where(l => l.CarrierId == c.Id).ToList();
            var completedLoads = carrierLoads.Where(l => l.Status == LoadStatus.Completed).ToList();

            return new CarrierReportResponse
            {
                CarrierId = c.Id,
                CompanyName = c.CompanyName,
                TotalLoads = carrierLoads.Count,
                AverageRate = carrierLoads.Count > 0
                    ? Math.Round(carrierLoads.Average(l => l.Rate), 2) : 0,
                AverageRPM = carrierLoads.Where(l => l.RatePerMile.HasValue).Count() > 0
                    ? Math.Round(carrierLoads.Where(l => l.RatePerMile.HasValue).Average(l => l.RatePerMile!.Value), 2) : 0,
                TotalRevenue = Math.Round(completedLoads.Sum(l => l.Rate), 2)
            };
        })
        .OrderByDescending(r => r.TotalRevenue)
        .ToList();

        return Ok(ApiResponse<List<CarrierReportResponse>>.Ok(report));
    }

    [HttpGet("dispatchers")]
    [Authorize(Policy = "reports.view")]
    public async Task<ActionResult<ApiResponse<List<DispatcherReportResponse>>>> GetDispatcherReport()
    {
        var loads = await _context.Loads
            .Where(l => !l.IsDeleted && l.DispatcherId.HasValue)
            .ToListAsync();

        var dispatcherIds = loads.Select(l => l.DispatcherId!.Value).Distinct().ToList();

        var users = await _context.Users
            .Where(u => dispatcherIds.Contains(u.Id))
            .ToListAsync();

        var report = dispatcherIds.Select(dispatcherId => {
            var dispatcherLoads = loads.Where(l => l.DispatcherId == dispatcherId).ToList();
            var completedLoads = dispatcherLoads.Where(l => l.Status == LoadStatus.Completed).ToList();
            var user = users.FirstOrDefault(u => u.Id == dispatcherId);

            return new DispatcherReportResponse
            {
                DispatcherId = dispatcherId,
                DispatcherName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                AssignedCarriers = dispatcherLoads.Select(l => l.CarrierId).Distinct().Count(),
                AssignedLoads = dispatcherLoads.Count,
                TotalRevenue = Math.Round(completedLoads.Sum(l => l.Rate), 2),
                CompletedLoads = completedLoads.Count
            };
        })
        .OrderByDescending(r => r.TotalRevenue)
        .ToList();

        return Ok(ApiResponse<List<DispatcherReportResponse>>.Ok(report));
    }
}
