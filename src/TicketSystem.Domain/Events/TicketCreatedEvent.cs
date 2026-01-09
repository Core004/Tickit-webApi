using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Events;

public class TicketCreatedEvent : IDomainEvent
{
    public int TicketId { get; }
    public string CreatedById { get; }
    public DateTime OccurredOn { get; }

    public TicketCreatedEvent(int ticketId, string createdById)
    {
        TicketId = ticketId;
        CreatedById = createdById;
        OccurredOn = DateTime.UtcNow;
    }
}
