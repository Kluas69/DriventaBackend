using Driventa.Domain.Common;

namespace Driventa.Domain.Entities;

public class Broker : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? McNumber { get; set; }
    public string? Address { get; set; }
    public int? InternalRating { get; set; }
    public string? PaymentNotes { get; set; }
    public string? GeneralNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Load> Loads { get; set; } = new List<Load>();
}