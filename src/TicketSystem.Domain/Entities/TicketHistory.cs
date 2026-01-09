using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketHistory : BaseEntity
{
    public int TicketId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedById { get; set; } = string.Empty;

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
