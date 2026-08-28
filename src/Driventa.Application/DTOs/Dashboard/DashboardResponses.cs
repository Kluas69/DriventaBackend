using Driventa.Domain.Enums;

namespace Driventa.Application.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    public int NewApplications { get; set; }
    public int ApplicationsInReview { get; set; }
    public int ActiveCarriers { get; set; }
    public int ActiveTrucks { get; set; }
    public int ActiveLoads { get; set; }
    public int LoadsInTransit { get; set; }
    public int CompletedLoadsThisMonth { get; set; }
    public decimal DispatchRevenueThisMonth { get; set; }
}

public class LoadStatusSummaryResponse
{
    public int Available { get; set; }
    public int Negotiating { get; set; }
    public int Booked { get; set; }
    public int Dispatched { get; set; }
    public int PickedUp { get; set; }
    public int InTransit { get; set; }
    public int Delivered { get; set; }
    public int Completed { get; set; }
}

public class RecentActivityResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ContactFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class RevenueSummaryResponse
{
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueThisYear { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingPayments { get; set; }
}