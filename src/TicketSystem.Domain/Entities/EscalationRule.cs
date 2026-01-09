using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class EscalationRule : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TriggerMinutes { get; set; }
    public EscalationAction Action { get; set; }
    public string? NotifyUserIds { get; set; } // Comma-separated user IDs
    public string? NotifyRoleIds { get; set; } // Comma-separated role IDs
    public string? EmailTemplate { get; set; }
    public TicketPriority? EscalateToPriority { get; set; }
    public bool IsActive { get; set; } = true;

    // Link to SLA Rule
    public int? SLARuleId { get; set; }
    public SLARule? SLARule { get; set; }

    // Conditions
    public int? PriorityId { get; set; }
    public int? CategoryId { get; set; }

    // Navigation properties
    public Priority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}
