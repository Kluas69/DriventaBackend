using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class PaymentConfig : BaseEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Payments");
        builder.Property(p => p.Amount).HasColumnType("decimal(12,2)");
        builder.Property(p => p.PaymentMethod).HasMaxLength(100);
        builder.Property(p => p.TransactionReference).HasMaxLength(500);

        builder.HasIndex(p => p.InvoiceId);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}