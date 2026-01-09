using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities;

public class TicketLink : AuditableEntity
{
    public int SourceTicketId { get; set; }
    public int TargetTicketId { get; set; }
    public TicketLinkType LinkType { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Ticket SourceTicket { get; set; } = null!;
    public Ticket TargetTicket { get; set; } = null!;
}
