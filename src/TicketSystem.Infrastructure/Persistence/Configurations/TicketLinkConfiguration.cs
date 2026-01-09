using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Configurations;

public class TicketLinkConfiguration : IEntityTypeConfiguration<TicketLink>
{
    public void Configure(EntityTypeBuilder<TicketLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.SourceTicket)
            .WithMany(t => t.SourceLinks)
            .HasForeignKey(l => l.SourceTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.TargetTicket)
            .WithMany(t => t.TargetLinks)
            .HasForeignKey(l => l.TargetTicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
