using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Entities;

public class TicketCustomField : BaseEntity
{
    public int TicketId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
    public string FieldType { get; set; } = "text";

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
}
