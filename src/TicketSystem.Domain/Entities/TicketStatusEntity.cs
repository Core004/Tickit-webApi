using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketStatusEntity : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsResolved { get; set; }
    public bool IsClosed { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
