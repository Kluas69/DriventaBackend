namespace Driventa.Application.DTOs.Reports;

public class LoadReportResponse
{
    public int TotalLoads { get; set; }
    public int ActiveLoads { get; set; }
    public int CompletedLoads { get; set; }
    public int CancelledLoads { get; set; }
    public decimal AverageRate { get; set; }
    public decimal AverageRPM { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class RevenueReportResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalDispatchFees { get; set; }
    public decimal TotalCarrierPayouts { get; set; }
    public decimal AverageRevenuePerLoad { get; set; }
    public List<MonthlyRevenue> MonthlyBreakdown { get; set; } = new();
}

public class MonthlyRevenue
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int LoadCount { get; set; }
}

public class CarrierReportResponse
{
    public Guid CarrierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalLoads { get; set; }
    public decimal AverageRate { get; set; }
    public decimal AverageRPM { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class DispatcherReportResponse
{
    public Guid DispatcherId { get; set; }
    public string DispatcherName { get; set; } = string.Empty;
    public int AssignedCarriers { get; set; }
    public int AssignedLoads { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedLoads { get; set; }
}