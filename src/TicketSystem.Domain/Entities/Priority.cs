using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Priority : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int Level { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    // SLA Configuration
    public int? ResponseTimeMinutes { get; set; }
    public int? ResolutionTimeMinutes { get; set; }

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<CompanyPriority> CompanyPriorities { get; set; } = new List<CompanyPriority>();
}
