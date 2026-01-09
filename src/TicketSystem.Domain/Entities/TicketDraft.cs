using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketDraft : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Foreign keys (optional for drafts)
    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }

    // Owner of the draft
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public TicketCategory? Category { get; set; }
    public Priority? Priority { get; set; }
    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public ApplicationUser? User { get; set; }
}
