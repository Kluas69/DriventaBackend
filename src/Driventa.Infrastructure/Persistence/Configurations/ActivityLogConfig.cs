using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class ActivityLogConfig : BaseEntityConfiguration<ActivityLog>
{
    public override void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("ActivityLogs");
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(2000);
        builder.Property(a => a.OldValuesJson).HasColumnType("jsonb");
        builder.Property(a => a.NewValuesJson).HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasMaxLength(50);

        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.EntityId);
    }
}