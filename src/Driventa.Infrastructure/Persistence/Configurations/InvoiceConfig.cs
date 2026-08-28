using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class InvoiceConfig : BaseEntityConfiguration<Invoice>
{
    public override void Configure(EntityTypeBuilder<Invoice> builder)
    {
        base.Configure(builder);

        builder.ToTable("Invoices");
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
        builder.Property(i => i.Subtotal).HasColumnType("decimal(12,2)");
        builder.Property(i => i.TaxAmount).HasColumnType("decimal(12,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(12,2)");

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.CarrierId);
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Carrier)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}