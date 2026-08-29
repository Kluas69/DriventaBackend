using Driventa.Application.DTOs.Common;
using Driventa.Application.DTOs.Invoices;
using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Driventa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly AppDbContext _context;

    public BillingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("invoices")]
    [Authorize(Policy = "billing.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<InvoiceResponse>>>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? carrierId = null)
    {
        var query = _context.Invoices
            .Include(i => i.Carrier)
            .Where(i => !i.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (carrierId.HasValue)
            query = query.Where(i => i.CarrierId == carrierId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceResponse
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CarrierId = i.CarrierId,
                CarrierName = i.Carrier.CompanyName,
                PeriodStart = i.PeriodStart,
                PeriodEnd = i.PeriodEnd,
                Subtotal = i.Subtotal,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                DueDate = i.DueDate,
                PaidAt = i.PaidAt,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<InvoiceResponse>>.Ok(
            new PaginatedResponse<InvoiceResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    [HttpPost("invoices")]
    [Authorize(Policy = "billing.create")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var carrier = await _context.Carriers
            .FirstOrDefaultAsync(c => c.Id == request.CarrierId && !c.IsDeleted);

        if (carrier == null)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Carrier not found."));

        var invoiceNumber = GenerateInvoiceNumber();
        var items = new List<InvoiceItem>();
        decimal subtotal = 0;

        foreach (var item in request.Items)
        {
            var amount = item.Quantity * item.UnitPrice;
            subtotal += amount;
            items.Add(new InvoiceItem
            {
                LoadId = item.LoadId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = amount
            });
        }

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CarrierId = request.CarrierId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Subtotal = subtotal,
            TaxAmount = request.TaxAmount,
            TotalAmount = subtotal + request.TaxAmount,
            Status = InvoiceStatus.Draft,
            DueDate = request.DueDate
        };

        foreach (var item in items)
        {
            invoice.Items.Add(item);
        }

        _context.Invoices.Add(invoice);

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Create",
            EntityType = "Invoice",
            EntityId = invoice.Id,
            Description = $"Invoice {invoice.InvoiceNumber} created for carrier {carrier.CompanyName}"
        });

        await _context.SaveChangesAsync();

        await _context.Entry(invoice).Reference(i => i.Carrier).LoadAsync();
        var response = MapToResponse(invoice);
        return Ok(ApiResponse<InvoiceResponse>.Ok(response, "Invoice created successfully."));
    }

    [HttpGet("invoices/{id:guid}")]
    [Authorize(Policy = "billing.view")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoiceById(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Carrier)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
            return NotFound(ApiResponse<InvoiceResponse>.Fail("Invoice not found."));

        return Ok(ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice)));
    }

    [HttpPost("invoices/{id:guid}/status")]
    [Authorize(Policy = "billing.manage")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoiceStatus(
        Guid id,
        [FromBody] InvoiceStatus status)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Carrier)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
            return NotFound(ApiResponse<InvoiceResponse>.Fail("Invoice not found."));

        invoice.Status = status;
        if (status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<InvoiceResponse>.Ok(MapToResponse(invoice), "Invoice status updated."));
    }

    [HttpPost("invoices/{id:guid}/payments")]
    [Authorize(Policy = "billing.create")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> AddPayment(
        Guid id,
        [FromBody] CreatePaymentRequest request)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
            return NotFound(ApiResponse<PaymentResponse>.Fail("Invoice not found."));

        var payment = new Payment
        {
            InvoiceId = id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = request.TransactionReference,
            Status = PaymentStatus.Completed,
            PaidAt = DateTimeOffset.UtcNow
        };

        _context.Payments.Add(payment);

        var totalPaid = await _context.Payments
            .Where(p => p.InvoiceId == id && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount) + request.Amount;

        if (totalPaid >= invoice.TotalAmount)
            invoice.Status = InvoiceStatus.Paid;
        else
            invoice.Status = InvoiceStatus.PartiallyPaid;

        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Payment",
            EntityType = "Invoice",
            EntityId = id,
            Description = $"Payment of ${request.Amount} recorded for invoice {invoice.InvoiceNumber}"
        });

        await _context.SaveChangesAsync();

        var response = new PaymentResponse
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            Status = payment.Status,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };

        return Ok(ApiResponse<PaymentResponse>.Ok(response, "Payment recorded successfully."));
    }

    [HttpGet("payments")]
    [Authorize(Policy = "billing.view")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PaymentResponse>>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? invoiceId = null)
    {
        var query = _context.Payments
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (invoiceId.HasValue)
            query = query.Where(p => p.InvoiceId == invoiceId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentResponse
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionReference = p.TransactionReference,
                Status = p.Status,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<PaymentResponse>>.Ok(
            new PaginatedResponse<PaymentResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }));
    }

    private static string GenerateInvoiceNumber()
    {
        var now = DateTimeOffset.UtcNow;
        var unique = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"INV-{now:yyyyMMddHHmmss}-{unique}";
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CarrierId = invoice.CarrierId,
            CarrierName = invoice.Carrier?.CompanyName,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            CreatedAt = invoice.CreatedAt,
            Items = invoice.Items?.Select(i => new InvoiceItemResponse
            {
                Id = i.Id,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Amount = i.Amount,
                LoadId = i.LoadId
            }).ToList() ?? new List<InvoiceItemResponse>()
        };
    }
}
