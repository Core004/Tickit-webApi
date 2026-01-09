using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class Holiday : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }

    // Optional: Company-specific holiday
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
