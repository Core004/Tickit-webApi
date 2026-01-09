using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.TicketNumber)
            .IsUnique();

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(5000);

        // Relationships
        builder.HasOne(t => t.CreatedBy)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Status)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.PriorityEntity)
            .WithMany(p => p.Tickets)
            .HasForeignKey(t => t.PriorityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Company)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Department)
            .WithMany(d => d.Tickets)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Team)
            .WithMany(t => t.Tickets)
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
