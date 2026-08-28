using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfig : BaseEntityConfiguration<InvoiceItem>
{
    public override void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("InvoiceItems");
        builder.Property(i => i.Description).IsRequired().HasMaxLength(500);
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(12,2)");
        builder.Property(i => i.Amount).HasColumnType("decimal(12,2)");

        builder.HasOne(i => i.Invoice)
            .WithMany(inv => inv.Items)
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Load)
            .WithMany()
            .HasForeignKey(i => i.LoadId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}