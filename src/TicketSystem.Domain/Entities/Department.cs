using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Department : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign keys
    public int? CompanyId { get; set; }
    public string? ManagerId { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public ApplicationUser? Manager { get; set; }
    public ICollection<DepartmentMember> Members { get; set; } = new List<DepartmentMember>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
