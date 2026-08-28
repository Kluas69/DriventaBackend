using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class DriverConfig : BaseEntityConfiguration<Driver>
{
    public override void Configure(EntityTypeBuilder<Driver> builder)
    {
        base.Configure(builder);

        builder.ToTable("Drivers");
        builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LastName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Email).HasMaxLength(200);
        builder.Property(d => d.Phone).HasMaxLength(50);
        builder.Property(d => d.LicenseNumber).HasMaxLength(100);
        builder.Property(d => d.LicenseState).HasMaxLength(50);

        builder.HasIndex(d => d.CarrierId);
        builder.HasIndex(d => d.Status);

        builder.HasOne(d => d.Carrier)
            .WithMany(c => c.Drivers)
            .HasForeignKey(d => d.CarrierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Truck)
            .WithMany()
            .HasForeignKey(d => d.TruckId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}