using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public TicketCategory? ParentCategory { get; set; }
    public ICollection<TicketCategory> SubCategories { get; set; } = new List<TicketCategory>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
