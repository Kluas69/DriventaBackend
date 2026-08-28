using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class ApplicationNoteConfig : BaseEntityConfiguration<ApplicationNote>
{
    public override void Configure(EntityTypeBuilder<ApplicationNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("ApplicationNotes");
        builder.Property(n => n.Content).IsRequired().HasMaxLength(5000);

        builder.HasOne(n => n.Application)
            .WithMany(a => a.Notes)
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}