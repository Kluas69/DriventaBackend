using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class BrokerConfig : BaseEntityConfiguration<Broker>
{
    public override void Configure(EntityTypeBuilder<Broker> builder)
    {
        base.Configure(builder);

        builder.ToTable("Brokers");
        builder.Property(b => b.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Email).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Phone).IsRequired().HasMaxLength(50);
        builder.Property(b => b.McNumber).HasMaxLength(50);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.PaymentNotes).HasMaxLength(2000);
        builder.Property(b => b.GeneralNotes).HasMaxLength(2000);
    }
}