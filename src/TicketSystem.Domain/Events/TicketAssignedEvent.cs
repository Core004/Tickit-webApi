using TicketSystem.Domain.Common;

namespace TicketSystem.Domain.Events;

public class TicketAssignedEvent : IDomainEvent
{
    public int TicketId { get; }
    public string AssignedToId { get; }
    public DateTime OccurredOn { get; }

    public TicketAssignedEvent(int ticketId, string assignedToId)
    {
        TicketId = ticketId;
        AssignedToId = assignedToId;
        OccurredOn = DateTime.UtcNow;
    }
}
