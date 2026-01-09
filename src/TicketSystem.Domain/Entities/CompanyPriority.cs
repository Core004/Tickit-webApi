using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class CompanyPriority : BaseEntity
{
    public int CompanyId { get; set; }
    public int PriorityId { get; set; }
    public int? ResponseTimeMinutes { get; set; }
    public int? ResolutionTimeMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Company Company { get; set; } = null!;
    public Priority Priority { get; set; } = null!;
}
