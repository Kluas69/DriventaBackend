namespace Driventa.Application.DTOs.Brokers;

public class CreateBrokerRequest
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
}

public class UpdateBrokerRequest
{
    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? McNumber { get; set; }
    public string? Address { get; set; }
    public int? InternalRating { get; set; }
    public string? PaymentNotes { get; set; }
    public string? GeneralNotes { get; set; }
    public bool? IsActive { get; set; }
}

public class BrokerResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? McNumber { get; set; }
    public string? Address { get; set; }
    public int? InternalRating { get; set; }
    public string? PaymentNotes { get; set; }
    public string? GeneralNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}