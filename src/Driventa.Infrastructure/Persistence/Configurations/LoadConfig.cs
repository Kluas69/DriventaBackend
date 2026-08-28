using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class LoadConfig : BaseEntityConfiguration<Load>
{
    public override void Configure(EntityTypeBuilder<Load> builder)
    {
        base.Configure(builder);

        builder.ToTable("Loads");
        builder.Property(l => l.LoadNumber).IsRequired().HasMaxLength(20);
        builder.Property(l => l.PickupCity).IsRequired().HasMaxLength(100);
        builder.Property(l => l.PickupState).IsRequired().HasMaxLength(50);
        builder.Property(l => l.DeliveryCity).IsRequired().HasMaxLength(100);
        builder.Property(l => l.DeliveryState).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Rate).HasColumnType("decimal(12,2)");
        builder.Property(l => l.RatePerMile).HasColumnType("decimal(8,2)");
        builder.Property(l => l.DispatchFeeType).HasMaxLength(50);
        builder.Property(l => l.DispatchFeeValue).HasColumnType("decimal(12,2)");
        builder.Property(l => l.DispatchFeeAmount).HasColumnType("decimal(12,2)");
        builder.Property(l => l.CarrierNetAmount).HasColumnType("decimal(12,2)");

        builder.HasIndex(l => l.LoadNumber).IsUnique();
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.CarrierId);

        builder.HasOne(l => l.Carrier)
            .WithMany(c => c.Loads)
            .HasForeignKey(l => l.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Truck)
            .WithMany()
            .HasForeignKey(l => l.TruckId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(l => l.Driver)
            .WithMany()
            .HasForeignKey(l => l.DriverId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(l => l.Broker)
            .WithMany(b => b.Loads)
            .HasForeignKey(l => l.BrokerId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}