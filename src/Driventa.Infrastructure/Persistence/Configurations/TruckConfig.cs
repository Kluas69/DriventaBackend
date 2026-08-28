using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class TruckConfig : BaseEntityConfiguration<Truck>
{
    public override void Configure(EntityTypeBuilder<Truck> builder)
    {
        base.Configure(builder);

        builder.ToTable("Trucks");
        builder.Property(t => t.TruckNumber).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Make).HasMaxLength(100);
        builder.Property(t => t.Model).HasMaxLength(100);
        builder.Property(t => t.LicensePlate).HasMaxLength(30);
        builder.Property(t => t.LicenseState).HasMaxLength(50);

        builder.HasIndex(t => t.CarrierId);
        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.Carrier)
            .WithMany(c => c.Trucks)
            .HasForeignKey(t => t.CarrierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}