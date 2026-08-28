using Driventa.Domain.Entities;
using Driventa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class ApplicationConfig : BaseEntityConfiguration<Domain.Entities.Application>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Application> builder)
    {
        base.Configure(builder);

        builder.ToTable("Applications");
        builder.Property(a => a.ApplicationNumber).IsRequired().HasMaxLength(20);
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Email).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(50);
        builder.Property(a => a.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.McNumber).HasMaxLength(50);
        builder.Property(a => a.DotNumber).HasMaxLength(50);
        builder.Property(a => a.PreferredLanes).HasMaxLength(500);
        builder.Property(a => a.AdditionalDetails).HasMaxLength(2000);

        builder.HasIndex(a => a.ApplicationNumber).IsUnique();
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Email);

        builder.HasOne(a => a.ConvertedCarrier)
            .WithOne(c => c.Application)
            .HasForeignKey<Carrier>(c => c.ApplicationId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}