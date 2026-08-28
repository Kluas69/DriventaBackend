using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class CarrierNoteConfig : BaseEntityConfiguration<CarrierNote>
{
    public override void Configure(EntityTypeBuilder<CarrierNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("CarrierNotes");
        builder.Property(n => n.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(n => n.Carrier)
            .WithMany(c => c.CarrierNotes)
            .HasForeignKey(n => n.CarrierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}