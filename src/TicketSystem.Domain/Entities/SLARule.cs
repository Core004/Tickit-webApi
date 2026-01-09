using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class SLARule : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Conditions
    public int? PriorityId { get; set; }
    public int? CategoryId { get; set; }
    public int? CompanyId { get; set; }

    // Navigation properties
    public Priority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
    public Company? Company { get; set; }
    public ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();
}
