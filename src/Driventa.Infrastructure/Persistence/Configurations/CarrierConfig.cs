using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class CarrierConfig : BaseEntityConfiguration<Carrier>
{
    public override void Configure(EntityTypeBuilder<Carrier> builder)
    {
        base.Configure(builder);

        builder.ToTable("Carriers");
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(50);
        builder.Property(c => c.McNumber).HasMaxLength(50);
        builder.Property(c => c.DotNumber).HasMaxLength(50);
        builder.Property(c => c.AddressLine1).HasMaxLength(200);
        builder.Property(c => c.AddressLine2).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(50);
        builder.Property(c => c.ZipCode).HasMaxLength(20);
        builder.Property(c => c.PreferredLanes).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.AssignedDispatcherId);
    }
}