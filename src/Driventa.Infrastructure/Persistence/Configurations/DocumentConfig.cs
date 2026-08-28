using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class DocumentConfig : BaseEntityConfiguration<Document>
{
    public override void Configure(EntityTypeBuilder<Document> builder)
    {
        base.Configure(builder);

        builder.ToTable("Documents");
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.StoredFileName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.FileUrl).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);

        builder.HasIndex(d => d.CarrierId);
        builder.HasIndex(d => d.LoadId);
        builder.HasIndex(d => d.DriverId);

        builder.HasOne(d => d.Carrier)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.CarrierId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(d => d.Load)
            .WithMany(l => l.Documents)
            .HasForeignKey(d => d.LoadId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(d => d.Driver)
            .WithMany()
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}