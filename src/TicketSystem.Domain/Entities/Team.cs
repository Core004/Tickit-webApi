using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Team : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign keys
    public int? DepartmentId { get; set; }
    public string? LeaderId { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public ApplicationUser? Leader { get; set; }
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
