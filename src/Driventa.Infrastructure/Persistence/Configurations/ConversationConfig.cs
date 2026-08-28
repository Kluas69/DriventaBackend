using Driventa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Driventa.Infrastructure.Persistence.Configurations;

public class ConversationConfig : BaseEntityConfiguration<Conversation>
{
    public override void Configure(EntityTypeBuilder<Conversation> builder)
    {
        base.Configure(builder);

        builder.ToTable("Conversations");
        builder.Property(c => c.VisitorId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.VisitorName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.VisitorEmail).HasMaxLength(200);
        builder.Property(c => c.VisitorPhone).HasMaxLength(50);

        builder.HasIndex(c => c.VisitorId);
        builder.HasIndex(c => c.IsActive);
    }
}