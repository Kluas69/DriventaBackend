using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class LoadNoteConfig : BaseEntityConfiguration<LoadNote>
{
    public override void Configure(EntityTypeBuilder<LoadNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("LoadNotes");
        builder.Property(n => n.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(n => n.Load)
            .WithMany(l => l.LoadNotes)
            .HasForeignKey(n => n.LoadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}